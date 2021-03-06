using NeuroSpeech.WebAtoms.Entity.Audit;
using NeuroSpeech.WebAtoms.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NeuroSpeech.WebAtoms.Entity
{
    public class ChangeSet
    {
        public IEnumerable<Change> Added { get; private set; }
        public IEnumerable<Change> Modified { get; private set; }
        public IEnumerable<Change> Deleted { get; private set; }

        public IEnumerable<Change> UpdatedEntities
        {
            get
            {
                foreach (var item in this.Added)
                {
                    yield return item;
                }
                foreach (var item in this.Modified)
                {
                    yield return item;
                }
            }
        }

        public ChangeSet(ObjectContext oc)
        {
            Added = oc.ObjectStateManager.GetObjectStateEntries(EntityState.Added).Select(x=> new Change(x.Entity,x.State)).ToList();
            Deleted = oc.ObjectStateManager.GetObjectStateEntries(EntityState.Deleted).Select(x => new Change(x.Entity, x.State)).ToList();
            Modified = oc.ObjectStateManager.GetObjectStateEntries(EntityState.Modified).Select(x=> new Change(x.Entity,x.State,x.OriginalValues,x.GetModifiedProperties().ToList() )).ToList();

        }

        public void BeginAudit()
        {
            Changes = new List<Change>();
            // add everything what was added first...
            Changes.AddRange(Added);
            Changes.AddRange(Modified);
            Changes.AddRange(Deleted);
            //foreach (var item in Added)
            //{
            //    var c = new Change(item.Entity, item.State);
            //    Changes.Add(c);
            //}
            //foreach (var item in Modified)
            //{
            //    var c = new Change(item.Entity, item.State, item.OriginalValues, item.GetModifiedProperties());
            //    Changes.Add(c);
            //}
            //foreach (var item in Deleted)
            //{
            //    var c = new Change(item.Entity, item.State);
            //    Changes.Add(c);
            //}
        }


        public List<Change> Changes { get; private set; }


        internal void EndAudit(IAuditContext ac)
        {
            foreach (var item in Changes.Where(x=> x.State == EntityState.Added))
            {
                Type type = item.Entity.GetType();
                List<string> keyValues = new List<string>();
                foreach (var k in type.GetEntityProperties(true))
                {
                    var key = k.GetValue(item.Entity);

                    var pv = item.Values.FirstOrDefault(x => x.Name == k.Name);
                    if (pv == null) {
                        pv = new CFieldValue { Name = k.Name };
                        item.Values.Add(pv);
                    }
                    pv.NewValue = key;

                    keyValues.Add(key.ToString());
                }

                item.Key = string.Join(",", keyValues);
            }

            List<CLinkValue> links = new List<CLinkValue>();
            foreach (var change in Changes)
            {
                foreach (var item in change.Values)
                {
                    foreach (var fk in item.Property.FKProperties)
                    {
                        CLinkValue v = new CLinkValue();
                        v.ObjectName = fk.EntityName;
                        var v1 = item.NewValue ?? item.OldValue;
                        if (v1 == null)
                            continue;
                        v.Key = v1.ToString();
                        if (string.IsNullOrWhiteSpace(v.Key))
                            continue;
                        v.ChildObject = item.Property.Property.DeclaringType.Name;
                        v.ChildKey = change.Key;
                        v.Operation = change.State.ToString();
                        links.Add(v);
                    }
                }
            }

            foreach (var item in links.GroupBy(x=>x.ObjectName))
            {
                string name = item.Key;
                foreach (var k in item.GroupBy(x=>x.Key).ToList())
                {
                    string key = k.Key;
                    Change c = Changes.FirstOrDefault(x => x.ObjectName == name && x.Key == key);
                    if (c != null)
                    {
                        c.Links.AddRange(k);
                    }
                    else {
                        c = new Change(EntityState.Modified);
                        c.ObjectName = name;
                        c.Key = key;
                        c.Links.AddRange(k);
                        Changes.Add(c);
                    }
                }
            }


            JavaScriptSerializer sr = new JavaScriptSerializer();

            foreach (var change in Changes)
            {
                if(change.Entity is IAuditIgnore)
                    continue;
                IAuditItem item = ac.CreateNew();
                item.Action = change.State.ToString();
                item.AuditTime = DateTime.UtcNow;
                item.IPAddress = ac.GetIPAddress();
                item.Username = ac.GetUsername();
                item.TableKey = change.Key;
                item.TableName = change.ObjectName;
                item.Fields = sr.Serialize(change.Values);
                item.Links = sr.Serialize(change.Links);
                ac.AddAudit(item);
            }

            ac.SaveChanges();
        }
    }


    public class Change {


        [XmlIgnore]
        [ScriptIgnore]
        public object Entity { get; private set; }

        public string ObjectName { get; set; }
        public string Key { get; set; }
        public EntityState State { get; set; }

        public List<CFieldValue> Values { get; private set; }
        public List<CLinkValue> Links { get; private set; }

        public Change(EntityState state)
        {
            State = state;
            Links = new List<CLinkValue>();
        }

        [XmlIgnore]
        [ScriptIgnore]
        public IEnumerable<string> ModifiedProperties { get; set; }

        public Change(object entity, EntityState state , DbDataRecord originalValues = null, IEnumerable<string> modifiedProperties = null)
        {
            this.Entity = entity;
            if (modifiedProperties != null)
            {
                this.ModifiedProperties = modifiedProperties.ToList();
            }
            Values = new List<CFieldValue>();
            Links = new List<CLinkValue>();

            Type type = entity.GetType();

            this.State = state;
            this.ObjectName = type.Name;

            if (state != EntityState.Added)
            {
                List<string> keyValues = new List<string>();
                foreach (var p in type.GetEntityProperties(true))
                {
                    keyValues.Add(p.GetValue(entity).ToString());
                }
                this.Key = string.Join("," , keyValues);
            }

            foreach (var p in type.GetEntityProperties())
            {
                var pv = new CFieldValue {
                    Name = p.Name,
                    Property = p
                };
                if (state == EntityState.Added)
                {
                    pv.NewValue = p.GetValue(entity);
                }
                else {
                    pv.OldValue = p.GetValue(entity);
                    if (originalValues != null) {
                        if (modifiedProperties.Any(x => p.Name == x))
                        {
                            pv.NewValue = originalValues[p.Name];
                        }
                        else {
                            continue;
                        }
                    }
                }
                Values.Add(pv);
            }



        }

    }

    public class CFieldValue {
        public string Name { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }

        [ScriptIgnore]
        [XmlIgnore]
        internal AtomPropertyInfo Property { get; set; }

    }

    public class CLinkValue {
        public string ObjectName { get; set; }
        public string Key { get; set; }
        public string ChildObject { get; set; }
        public string ChildKey { get; set; }
        public string Operation { get; set; }
        public string ChildProperty { get; set; }
    }


}
