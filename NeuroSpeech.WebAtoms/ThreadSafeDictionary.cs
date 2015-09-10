using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.WebAtoms
{
    public class ThreadSafeDictionary<TKey,TValue>: IEnumerable<KeyValuePair<TKey,TValue>>
    {

        private ConcurrentDictionary<TKey, TValue> InternalDictionary = new ConcurrentDictionary<TKey, TValue>();

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory) {

            return InternalDictionary.GetOrAdd(key,fkey=> SingleTon(fkey,factory) );
        }

        private object lockObject = new object();

        private TValue SingleTon(TKey fkey, Func<TKey, TValue> factory)
        {
            lock (lockObject)
            {
                TValue v;
                if (InternalDictionary.TryGetValue(fkey, out v))
                {
                    return v;
                }
                v = factory(fkey);
                InternalDictionary.TryAdd(fkey, v);
                return v;
            }
        }

        public bool TryGetValue(TKey key, out TValue v)
        {
            return InternalDictionary.TryGetValue(key, out v);
        }

        public TValue this[TKey key]{
            get{
                TValue t;
                InternalDictionary.TryGetValue(key, out t);
                return t;
            }
            set {
                //InternalDictionary.TryUpdate(key, value, this[key]);
                InternalDictionary[key] = value;
            }
        }


        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return InternalDictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
