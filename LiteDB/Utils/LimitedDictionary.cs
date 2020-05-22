using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Utils
{
    [DebuggerTypeProxy(typeof(IDictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class LimitedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {

        private readonly OrderedDictionary dict;
        private readonly object lockObj;

        public int Capacity { get; private set; }

        public LimitedDictionary(int maxCapaxity)
        {
            Capacity = maxCapaxity;
            dict = new OrderedDictionary(Capacity);
            lockObj = new object();
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (lockObj)
                {
                    var value = dict[key];
                    if (value == null)
                        throw new KeyNotFoundException(string.Format("The given key '{0}' was not present in the dictionary.", key));
                    return (TValue)value;
                }

            }
            set
            {
                bool contains;
                lock (lockObj)
                    contains = dict.Contains(key);

                if (contains)
                    lock (lockObj)
                        dict[key] = value;
                else
                    Add(key, value);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                lock (lockObj)
                    return dict.Keys.Cast<TKey>().ToList();
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                lock (lockObj)
                    return dict.Values.Cast<TValue>().ToList();
            }
        }

        public int Count
        {
            get
            {
                lock (lockObj)
                    return dict.Count;
            }
        }

        public bool IsReadOnly => dict.IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            lock (lockObj)
            {
                if (Capacity == Count)
                    dict.RemoveAt(0);

                dict.Add(key, value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            lock (lockObj)
                dict.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) => TryGetValue(item.Key, out TValue value) && item.Value.Equals(value);

        public bool ContainsKey(TKey key)
        {
            lock (lockObj)
                return dict.Contains(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (lockObj)
            {
                if (array.Length - arrayIndex < dict.Count)
                    throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");

                var i = arrayIndex;

                foreach (DictionaryEntry entry in dict)
                {
                    var kvp = new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value);
                    array[i++] = kvp;
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this) as IEnumerator<KeyValuePair<TKey, TValue>>;

        public bool Remove(TKey key)
        {
            lock (lockObj)
            {
                if (dict.Contains(key))
                {
                    dict.Remove(key);
                    return true;
                }

                return false;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item))
            {
                lock (lockObj)
                    dict.Remove(item.Key);
                return true;
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (lockObj)
            {
                if (dict.Contains(key))
                {
                    value = (TValue)dict[key];
                    return true;
                }

                value = default;
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        internal sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private KeyValuePair<TKey, TValue> current;
            public KeyValuePair<TKey, TValue> Current => current;

            object IEnumerator.Current => current;

            private IEnumerator ordDictEnumerator;
            private LimitedDictionary<TKey, TValue> dict;


            internal Enumerator(LimitedDictionary<TKey, TValue> dict)
            {
                this.dict = dict;
                ordDictEnumerator = dict.dict.GetEnumerator();
                current = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                if (!ordDictEnumerator.MoveNext())
                    return false;

                var currentEntry = (DictionaryEntry)ordDictEnumerator.Current;
                current = new KeyValuePair<TKey, TValue>((TKey)currentEntry.Key, (TValue)currentEntry.Value);
                return true;
            }

            public void Reset()
            {
                ordDictEnumerator = dict.dict.GetEnumerator();
                current = default;
            }
        }
    }

    internal sealed class IDictionaryDebugView<K, V>
    {
        private readonly IDictionary<K, V> _dictionary;

        public IDictionaryDebugView(IDictionary<K, V> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            _dictionary = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<K, V>[] Items
        {
            get
            {
                KeyValuePair<K, V>[] items = new KeyValuePair<K, V>[_dictionary.Count];
                _dictionary.CopyTo(items, 0);
                return items;
            }
        }
    }
}
