using NeuroSpeech.WebAtoms.Entity.Audit;
using NeuroSpeech.WebAtoms.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NeuroSpeech.WebAtoms.Entity
{




    public abstract class AtomObjectContext : ObjectContext, ISecureRepository
    {

        public AtomObjectContext(EntityConnection connection) :
            base(connection)
        {
            Initialize();                
        }

        public AtomObjectContext(string connectionString)
            : base(connectionString)
        {
            Initialize();                
        }

        public AtomObjectContext(EntityConnection connection, string defaultContainerName)
            : base(connection, defaultContainerName)
        {
            Initialize();
        }

        public AtomObjectContext(string connectionString, string defaultContainerName)
            : base(connectionString, defaultContainerName)
        {
            Initialize();
        }


        protected virtual void Initialize() {

            this.ObjectMaterialized += AtomObjectContext_ObjectMaterialized;
        }


        void AtomObjectContext_ObjectMaterialized(object sender, ObjectMaterializedEventArgs e)
        {
            AtomEntity ae = e.Entity as AtomEntity;
            ae.ObjectContext = this;
        }


        public WrappedTransaction CreateTransaction(System.Transactions.IsolationLevel level = System.Transactions.IsolationLevel.Serializable,
            TimeSpan? t = null)
        {
            if (t == null) t = TimeSpan.FromMinutes(1);
            return new WrappedTransaction(level,t);
        }

        public WrappedTransaction CreateTransactionAsync(System.Transactions.IsolationLevel level = System.Transactions.IsolationLevel.Serializable,
            TimeSpan? t = null)
        {
            if (t == null) t = TimeSpan.FromMinutes(1);
            return new WrappedTransaction(level, t, true);
        }
        public class WrappedTransaction : IDisposable
        {

            public TransactionScope Scope { get; private set; }

            public WrappedTransaction(System.Transactions.IsolationLevel isolation = System.Transactions.IsolationLevel.Serializable, TimeSpan? t = null): this(isolation, t, false)
            {
                
            }

            public WrappedTransaction(System.Transactions.IsolationLevel isolation = System.Transactions.IsolationLevel.Serializable, TimeSpan? t = null, bool asyncEnabled = false)
            {
                if (Transaction.Current == null)
                {
                    if (asyncEnabled)
                    {
                        Scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                        {
                            IsolationLevel = isolation,
                            Timeout = t == null ? TimeSpan.FromMinutes(1) : t.Value
                        }, TransactionScopeAsyncFlowOption.Enabled);

                    }
                    else
                    {
                        Scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                        {
                            IsolationLevel = isolation,
                            Timeout = t == null ? TimeSpan.FromMinutes(1) : t.Value
                        });
                    }
                }
            }

            public void Complete()
            {
                if (Scope != null)
                {
                    Scope.Complete();
                }
            }

            public void Dispose()
            {
                if(Scope!=null)
                {
                    Scope.Dispose();
                }
            }
        }
        

        public IAuditContext AuditContext { get; set; }


        private void ValidateChanges(ChangeSet cs)
        {

            List<ValidationResult> results = new List<ValidationResult>();

            foreach (var item in cs.Added)
            {
                ValidationContext vc = new ValidationContext(item.Entity, null, null);
                Validator.TryValidateObject(item.Entity, vc, results, true);
            }

            foreach (var item in cs.Modified)
            {
                // only validate modified properties...
                ValidationContext vc = new ValidationContext(item.Entity, null, null);
                Type type = item.Entity.GetType();
                foreach (var property in item.ModifiedProperties)
                {
                    vc.MemberName = property;
                    PropertyInfo p = type.GetProperty(property);
                    object val = p.GetValue(item.Entity, null);
                    Validator.TryValidateProperty(val, vc, results);
                }
            }

            if (results.Any())
            {
                throw new ValidationException(string.Join(",", results.Select(x => x.ErrorMessage)));
            }
        }

        List<Action> postSaveActions;
        public List<Action> PostSaveActions
        {
            get
            {
                return (postSaveActions ?? (postSaveActions = new List<Action>()));
            }
            private set
            {
                postSaveActions = value;
            }
        }

        public override async Task<int> SaveChangesAsync(SaveOptions options, CancellationToken cancellationToken)
        {
            ChangeSet cs = new ChangeSet(this);

            OnBeforeValidate(cs);

            ValidateChanges(cs);

            int results = 0;

            using (var scope = CreateTransactionAsync())
            {

                // for modify and add , validate properties...
                if (SecurityContext != null)
                {
                    SecurityContext.ValidateBeforeSave(this, cs);
                }

                // Uncomment following line to turn on the Auditing

                OnBeforeSave(cs);

                if (AuditContext != null)
                {
                    cs.BeginAudit();
                }

                results = await base.SaveChangesAsync(options, cancellationToken);

                OnAfterSave(cs);

                if (AuditContext != null)
                {
                    cs.EndAudit(AuditContext);
                }


                if (SecurityContext != null)
                {
                    SecurityContext.ValidateAfterSave(this, cs);
                }


                scope.Complete();
            }

            if (PostSaveActions.Count > 0)
            {
                var list = PostSaveActions.ToList();
                PostSaveActions.Clear();
                foreach (var item in list)
                {
                    item();
                }
            }

            return results;
        }

        public override int SaveChanges(SaveOptions options)
        {

            ChangeSet cs = new ChangeSet(this);

            OnBeforeValidate(cs);

            ValidateChanges(cs);

            int results = 0;

            using (var scope = CreateTransaction())
            {

                // for modify and add , validate properties...
                if (SecurityContext != null)
                {
                    SecurityContext.ValidateBeforeSave(this, cs);
                }

                // Uncomment following line to turn on the Auditing

                OnBeforeSave(cs);

                if (AuditContext != null) {
                    cs.BeginAudit();
                }

                results = base.SaveChanges(options);

                OnAfterSave(cs);

                if (AuditContext != null)
                {
                    cs.EndAudit(AuditContext);
                }


                if (SecurityContext != null) {
                    SecurityContext.ValidateAfterSave(this,cs);
                }


                scope.Complete();
            }

            if (PostSaveActions.Count > 0)
            {
                var list = PostSaveActions.ToList();
                PostSaveActions.Clear();
                foreach (var item in list)
                {
                    item();
                }
            }

            return results;
        }


        protected virtual void OnBeforeValidate(ChangeSet cs)
        {
            
        }

        protected virtual void OnAfterSave(ChangeSet cs)
        {
            
        }

        protected virtual void OnBeforeSave(ChangeSet cs)
        {
            
        }


        public BaseSecurityContext SecurityContext
        {
            get;
            set;
        }

        //public virtual ObjectQuery<T> ApplyFilter<T>(ObjectQuery<T> oset) where T : class
        //{
        //    if (this.SecurityContext != null)
        //    {
        //        return SecurityContext.ApplyFilter<T>(oset);
        //    }
        //    return oset;
        //}

        //public ObjectSet<T> GetObjectSet<T>()
        //    where T : class
        //{
        //    Type setType = typeof(ObjectSet<T>);
        //    foreach (PropertyInfo p in this.GetType().GetProperties())
        //    {
        //        if (p.PropertyType == setType)
        //        {
        //            return p.GetValue(this, null) as ObjectSet<T>;
        //        }
        //    }
        //    return null;
        //}

        //public T GetStoreObject<T>(T copy)
        //           where T : class
        //{
        //    ObjectSet<T> objectSet = GetObjectSet<T>();

        //    ParameterExpression pe = Expression.Parameter(typeof(T));

        //    Expression ex = null;

        //    Type type = typeof(T);

        //    foreach (var item in type.GetEntityProperties(true))
        //    {
        //        Expression ce = Expression.Equal(
        //            Expression.Property(pe, item),
        //            Expression.Constant(item.GetValue(copy, null))
        //            );
        //        if (ex == null)
        //            ex = ce;
        //        else
        //        {
        //            ex = Expression.AndAlso(ex, ce);
        //        }
        //    }

        //    Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(ex, pe);

        //    return objectSet.FirstOrDefault(predicate);
        //}


        //public object GetStoreObjectByKey(object copy)
        //{
        //    return GenericMethods.InvokeGeneric(this, "GetStoreObject", copy.GetType(), copy);
        //}

        //public dynamic GetObjectSet(Type dataType)
        //{
        //    Type setType = typeof(ObjectSet<>).MakeGenericType(dataType);
        //    foreach (var item in this.GetType().GetProperties())
        //    {
        //        if (item.PropertyType == setType)
        //        {
        //            return item.GetValue(this, null);
        //        }
        //    }

        //    return null;
        //}

        ////public Object GetObjectSet(string dataType)
        ////{
        ////    foreach (var item in this.GetType().GetProperties())
        ////    {
        ////        if (item.PropertyType.IsGenericType && item.PropertyType.GetGenericArguments()[0].Name == dataType)
        ////        {
        ////            return item.GetValue(this, null);
        ////        }
        ////    }

        ////    return null;
        ////}

        //public object GetObjectByKey(Type type, object key)
        //{
        //    object f = Activator.CreateInstance(type);
        //    var p = type.GetEntityProperties(true).FirstOrDefault();
        //    p.SetValue(f, key, null);

        //    return GenericMethods.InvokeGeneric(this, "GetStoreObject", type, f);
        //}

        //[Obsolete("User Where<T> Instead")]
        public IQueryable<T> ApplyFilter<T>(IQueryable<T> q) where T:class {
            if (SecurityContext != null)
            {
                Expression<Func<T, bool>> f = SecurityContext.GetReadRule<T>(this);
                return q.Where(f);
            }
            return q;
        }

        public T FirstOrDefault<T>(Expression<Func<T,bool>> predicate = null) where T:class {
            if (predicate == null)
                return Query<T>().FirstOrDefault();
            return Query<T>().FirstOrDefault(predicate);
        }

        public IQueryable<T> Where<T>(Expression<Func<T,bool>> q) where T : class 
        {
            if (q == null)
                return Query<T>();
            return Query<T>().Where(q);
        }

        private Dictionary<Type, object> _objectSets = new Dictionary<Type, object>();

        public ObjectSet<T> GetObjectSet<T>() where T:class {
            object val = null;
            Type t =typeof(T);
            if (_objectSets.TryGetValue(t, out val))
                return (ObjectSet<T>)val;
            var p = this.GetType().GetCachedProperties().FirstOrDefault(x => x.PropertyType == typeof(ObjectSet<T>));
            val = p.GetValue(this,null);
            return (ObjectSet<T>)val;
        }

        public IQueryable<T> Query<T>() where T : class
        {
            IQueryable<T> o = GetObjectSet<T>();
            return this.ApplyFilter<T>(o);
        }

        public IQueryable Query(Type t)
        {
            return (IQueryable)GenericMethods.InvokeGeneric(this, "Query", t, null);
        }

        public T LoadEntity<T>(T keyObject) where T:class
        {

            Type t = typeof(T);
            if (t != keyObject.GetType())
            {
                return (T)GenericMethods.InvokeGeneric(this, "LoadEntity", keyObject.GetType(), keyObject);
            }
            IQueryable<T> q = Query<T>();
            ParameterExpression exp = Expression.Parameter(t);
            Expression r = null;
            foreach (var p in t.GetEntityProperties(true))
            {
                
                var c = Expression.Equal(Expression.Property(exp, p.Property), Expression.Constant(p.GetValue(keyObject)));
                if (r == null)
                {
                    r = c;
                }
                else {
                    r = Expression.And(r, c);
                }
            }
            Expression<Func<T, bool>> l = Expression.Lambda<Func<T, bool>>(r, exp);
            return q.FirstOrDefault(l);
        }

        public IQueryable<T> QueryByKey<T>(object key) where T:class
        {
            Type type = typeof(T);
            IQueryable<T> q = Query<T>();
            ParameterExpression pe = Expression.Parameter(type);
            var p = type.GetEntityProperties(true).FirstOrDefault();
            if (p.PropertyType.IsValueType && p.PropertyType != key.GetType()) {
                key = Convert.ChangeType(key, p.PropertyType);
            }
            Expression c = Expression.Equal(Expression.Property(pe, p.Property), Expression.Constant(key));
            Expression<Func<T,bool>> l = Expression.Lambda<Func<T,bool>>(c, pe);
            return q.Where(l);
        }

        public T LoadByKey<T>(object key) where T : class {
            return QueryByKey<T>(key).FirstOrDefault();
        }

        public object LoadEntityByKey(Type type, object key)
        {
            return GenericMethods.InvokeGeneric(this, "LoadByKey", type, key);
        }


        public T ModifyEntity<T>(T entity) where T : class
        {
            var o = GetObjectSet<T>();
            T storeEntity = LoadEntity<T>(entity);
            if (storeEntity == null)
            {
                o.AddObject(entity);
                return entity;
            }
            else { 
                // merge properties...
                FastCloner.Merge(storeEntity, entity);
            }
            return storeEntity;
        }

        public void AddEntity<T>(T entity) where T : class {
            GetObjectSet<T>().AddObject(entity);
            AtomEntity ae = entity as AtomEntity;
            if (ae != null) {
                ae.ObjectContext = this;
            }
        }

        public void AddEntity(object entity) {
            GenericMethods.InvokeGeneric(this, "AddEntity", entity.GetType(), entity);
        }

        public object ModifyEntity(object entity)
        {
            return GenericMethods.InvokeGeneric(this, "ModifyEntity", entity.GetType(), entity);
        }

        public object DeleteEntity<T>(T entity) where T : class 
        {
            var o = GetObjectSet<T>();
            var se = LoadEntity<T>(entity);
            if (se != null) {
                o.DeleteObject(se);
                return se;
            }
            return null;
        }

        object ISecureRepository.DeleteEntity(object entity)
        {
            return GenericMethods.InvokeGeneric(this, "DeleteEntity", entity.GetType(), entity);
        }

        public int Save()
        {
            return this.SaveChanges();
        }



        public ObjectStateEntry GetEntry(object entity)
        {
            return ObjectStateManager.GetObjectStateEntry(entity);
        }
    }

    public class FastCloner {

        static ThreadSafeDictionary<Type, Action<object, object>> Cache = new ThreadSafeDictionary<Type, Action<object, object>>();

        internal static void Merge(object storeEntity, object entity)
        {
            Type t = entity.GetType();
            Action<object,object> cloner = Cache.GetOrAdd(t, CreateCloner);
            cloner(storeEntity, entity);
        }

        internal static Action<object, object> CreateCloner(Type t) {

            List<Expression> statements = new List<Expression>();

            ParameterExpression pLeft = Expression.Parameter(typeof(object));
            ParameterExpression pRight = Expression.Parameter(typeof(object));

            Expression left = Expression.Variable(t, "left");
            Expression right = Expression.Variable(t,"right");

            statements.Add( Expression.Assign(left, Expression.TypeAs(pLeft,t)));
            statements.Add(Expression.Assign(right, Expression.TypeAs(pRight,t)));

            foreach (var prop in t.GetCachedProperties())
            {
                statements.Add(Expression.Assign(Expression.Property(left, prop), Expression.Property(right, prop)));
            }

            BlockExpression block = Expression.Block(statements.ToArray());

            Expression<Action<object, object>> l = Expression.Lambda<Action<object, object>>(block, pLeft, pRight);

            return l.Compile();
        }
    }
}
