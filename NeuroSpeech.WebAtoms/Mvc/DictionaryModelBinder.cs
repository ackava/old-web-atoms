using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace NeuroSpeech.WebAtoms.Mvc
{
    public class DictionaryModelBinder
    {

        private static ThreadSafeDictionary<Type, Func<object, object>> Binders = new ThreadSafeDictionary<Type, Func<object, object>>();


        public static void Bind(object model, IDictionary<string,object> values)
        {
            Type type = model.GetType();
            foreach (var item in values)
            {
                PropertyInfo p = type.GetCachedProperty(item.Key);
                if (p == null)
                    continue;
                if (!p.CanWrite)
                    continue;
                object val = item.Value;
                if (val != null)
                {
                    Type pt = p.PropertyType;

                    if (pt.IsGenericType)
                    {
                        if (pt.GetGenericTypeDefinition() == typeof(System.Nullable<>))
                        {
                            pt = pt.GetGenericArguments()[0];
                        }
                    }
                    if (pt == typeof(Guid))
                    {
                        val = Guid.Parse((string)val);
                    }
                    if (val is IDictionary<string, object>)
                    {
                        var src = p.GetOrCreatePropertyValue(model);
                        if (src != null)
                        {
                            Bind(src, (IDictionary<string, object>)val);
                        }
                        continue;
                    }
                    else if (val is System.Collections.IList) {

                        Type objectType = p.PropertyType;
                        if (p.PropertyType.IsArray)
                        {
                            objectType = p.PropertyType.GetElementType();
                        }
                        else {
                            objectType = p.PropertyType.GetGenericArguments()[0];
                        }

                        var src = p.GetOrCreatePropertyValue(model);
                        if (src != null) {
                            var iList = src as System.Collections.IList;
                            if (iList != null)
                            {
                                foreach (object childItem in (System.Collections.IList)val)
                                {
                                    if (childItem is IDictionary<string, object> d) {
                                        var child = Activator.CreateInstance(objectType);
                                        iList.Add(child);
                                        Bind(child, childItem as IDictionary<string, object>);
                                    }
                                    else
                                    {
                                        iList.Add(childItem);
                                    }
                                    //iList.Add(childItem);
                                }
                            }
                        }
                        continue;
                    }else
                    {
                        try
                        {
                            val = Convert.ChangeType(val, pt);
                        }
                        catch (Exception ex) {
                            throw new InvalidOperationException("Failed to convert value " + val + " to " + pt.FullName + " for " + p.Name, ex);
                        }
                    }
                }

                object oldValue = p.GetValue(model, null);

                if (oldValue == val)
                    continue;
                if (oldValue != null && val != null && val.Equals(oldValue))
                    continue;

                p.SetValue(model, val, null);
            }

        }



    }

    //public class DictionaryBinder
    //{
    //    public Type ObjectType { get; private set; }

    //    public DictionaryBinder(Type objectType)
    //    {
    //        ObjectType = objectType;
    //    }

    //    ThreadSafeDictionary<string, Action<object>> PropertySetters = new ThreadSafeDictionary<string, Action<object>>();

    //    public void SetValue(object target, IDictionary<string,object> values)
    //    {
    //        foreach (var item in values)
    //        {
    //            string key = item.Key;
    //            object value = item.Value;

    //            if (value == null)
    //            {
                    
    //            }
    //        }
    //    }


    //    static Dictionary<Type, Func<object, object>> Converters = new Dictionary<Type, Func<object, object>>()
    //    {
    //        {
    //            typeof(Guid), f => { 
    //                return Guid.Parse(f.ToString()); 
    //            }
    //        },
    //        {
    //            typeof(Guid?), f => { 
    //                return Guid.Parse(f.ToString()); 
    //            }
    //        },
    //        {
    //            typeof(DateTime), f =>{
    //                return WebAtomsMvcHelper.ToDateTime(f.ToString());
    //            }
    //        },
    //        {
    //            typeof(DateTime?), f =>{
    //                return WebAtomsMvcHelper.ToDateTime(f.ToString());
    //            }
    //        }
    //    };




    //}

    //public interface IBindingValueConverter
    //{
    //    object Convert(object input);
    //}

    //public class BindingValueConverter<T>
    //{
    //    Func<object, T> converter;

    //    public BindingValueConverter(Func<object,T> c)
    //    {
    //        converter = c;
    //    }
    //}

}
