using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Implement a simple memory cache removing oldest value when ritch limit. This class are not thread safe
    /// </summary>
    internal class Cache<TKey, TValue>
    {
        private readonly int _limit;
        private int _count = 0;

        private LinkedList<TKey> _index = new LinkedList<TKey>();
        private Dictionary<TKey, TValue> _values = new Dictionary<TKey, TValue>();

        public Cache(int limit)
        {
            _limit = limit;
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> addFactory)
        {
            if (_values.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                if (_count >= _limit)
                {
                    var last = _index.Last();
                    _index.RemoveLast();
                    _values.Remove(last);
                    _count--;
                }

                // execute add factory to get value
                value = addFactory(key);

                // add on index at top of list
                _index.AddFirst(new LinkedListNode<TKey>(key));

                // add indexed value
                _values.Add(key, value);

                _count++;

                return value;
            }
        }
    }
}
