using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public class BsonDocument : IDictionary<string, BsonValue>, IComparable<BsonValue>
    {
        private Dictionary<string, BsonValue> _dictionary;

        public BsonDocument()
        {
            _dictionary = new Dictionary<string, BsonValue>(StringComparer.OrdinalIgnoreCase);
            Length = 5;
        }

        public BsonDocument(ConcurrentDictionary<string, BsonValue> dict)
            : this(new Dictionary<string, BsonValue>(dict)) { }

        public BsonDocument(Dictionary<string, BsonValue> dict)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));

            Length = 5;

            _dictionary = dict;

            foreach (var item in _dictionary)
                Length += GetBytesCountElement(item.Key, item.Value);
        }

        /// <summary>
        /// Get/Set position of this document inside database. It's filled when used in Find operation.
        /// </summary>
        internal PageAddress RawId { get; set; } = PageAddress.Empty;

        /// <summary>
        /// Get/Set a field for document. Fields are case sensitive
        /// </summary>
        public BsonValue this[string name]
        {
            get => _dictionary.GetOrDefault(name, BsonValue.Null);
            set
            {
                if (_dictionary.ContainsKey(name))
                    Length -= GetBytesCountElement(name, _dictionary[name]);

                _dictionary[name] = value ?? BsonValue.Null;
                Length += GetBytesCountElement(name, value);
            }
        }

        #region Length

        public int Length;

        private int GetBytesCountElement(string key, BsonValue value)
        {
            return
                1 + // element type
                Encoding.UTF8.GetByteCount(key) + // CString
                1 + // CString 0x00
                value?.Length ?? 0 +
                (value.Type == BsonType.String || value.Type == BsonType.Binary || value.Type == BsonType.Guid ? 5 : 0); // bytes.Length + 0x??
        }

        #endregion

        #region Update support with expressions

        /// <summary>
        /// Get an IEnumerable of values from a json-like path inside document. Use BsonExpression to parse this path
        /// </summary>
        public IEnumerable<BsonValue> Get(string path, bool includeNullIfEmpty = false)
        {
            var expr = BsonExpression.Create(path);

            return expr.Execute(this, includeNullIfEmpty);
        }

        /// <summary>
        /// Copy all properties from other document inside this current document
        /// </summary>
        public BsonDocument Extend(BsonDocument other)
        {
            foreach (var key in other._dictionary.Keys)
            {
                if (_dictionary.ContainsKey(key))
                    Length -= GetBytesCountElement(key, _dictionary[key]);

                _dictionary[key] = other._dictionary[key];

                Length += GetBytesCountElement(key, _dictionary[key]);
            }

            return this;
        }

        #endregion

        #region CompareTo / ToString

        public int CompareTo(BsonValue other)
        {
            // if types are different, returns sort type order
            if (other.Type != BsonType.Document)
                return BsonType.Document.CompareTo(other.Type);

            var thisKeys = this.Keys.ToArray();
            var thisLength = thisKeys.Length;

            var otherKeys = other.DocValue.Keys.ToArray();
            var otherLength = otherKeys.Length;

            var result = 0;
            var i = 0;
            var stop = Math.Min(thisLength, otherLength);

            for (; 0 == result && i < stop; i++)
                result = this[thisKeys[i]].CompareTo(other.DocValue[thisKeys[i]]);

            // are different
            if (result != 0) return result;

            // test keys length to check which is bigger
            if (i == thisLength) return i == otherLength ? 0 : -1;
            return 1;
        }

        public override string ToString() => JsonSerializer.Serialize(this);

        #endregion

        #region IDictionary

        public IEnumerable<string> KeysOrdered
        {
            get
            {
                if (_dictionary.ContainsKey("_id"))
                    yield return "_id";

                foreach (var key in _dictionary.Keys.Where(x => x != "_id"))
                    yield return key;
            }
        }

        public ICollection<string> Keys => _dictionary.Keys;


        public ICollection<BsonValue> Values => _dictionary.Values;


        public int Count => _dictionary.Count;


        public bool IsReadOnly => false;


        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);


        public void Add(string key, BsonValue value) => this[key] = value;


        public bool Remove(string key)
        {
            if (_dictionary.ContainsKey(key))
                Length -= GetBytesCountElement(key, _dictionary[key]);

            return _dictionary.Remove(key);
        }


        public bool TryGetValue(string key, out BsonValue value) => _dictionary.TryGetValue(key, out value);


        public void Add(KeyValuePair<string, BsonValue> item) => this[item.Key] = item.Value;


        public void Clear()
        {
            Length = 5;
            _dictionary.Clear();
        }


        public bool Contains(KeyValuePair<string, BsonValue> item) => _dictionary.Contains(item);


        public void CopyTo(KeyValuePair<string, BsonValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, BsonValue>>)_dictionary).CopyTo(array, arrayIndex);


        public void CopyTo(BsonDocument doc)
        {
            foreach (var key in _dictionary.Keys)
                doc[key] = _dictionary[key];
        }

        public bool Remove(KeyValuePair<string, BsonValue> item) => Remove(item.Key);


        public IEnumerator<KeyValuePair<string, BsonValue>> GetEnumerator() => _dictionary.GetEnumerator();


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        public static implicit operator Dictionary<string, BsonValue>(BsonDocument value) => value._dictionary;


        public override int GetHashCode() => _dictionary.GetHashCode();

        public override bool Equals(object obj) => CompareTo(new BsonValue(obj)) == 0;
    }
}
