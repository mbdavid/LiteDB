using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public class BsonDocument : BsonValue, IDictionary<string, BsonValue>, IComparable<BsonValue>
    {
        private Dictionary<string, BsonValue> _dictionary;

        public BsonDocument()
        {
            Type = BsonType.Document;
            _dictionary = new Dictionary<string, BsonValue>(StringComparer.OrdinalIgnoreCase);
            Length = 5;
            _docValue = this;
        }

        public BsonDocument(ConcurrentDictionary<string, BsonValue> dict)
            : this(new Dictionary<string, BsonValue>(dict)) { }

        public BsonDocument(Dictionary<string, BsonValue> dict)
        {
            _dictionary = dict ?? throw new ArgumentNullException(nameof(dict));

            Type = BsonType.Document;

            Length = 5;

            foreach (var item in _dictionary)
            {
                Length += GetBytesCountElement(item.Key, item.Value);
                if (item.Value.IsArray || item.Value.IsDocument)
                    item.Value.LengthChanged += OnLengthChanged;
            }

            _docValue = this;
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
                BsonValue item;
                if (_dictionary.ContainsKey(name))
                {
                    item = _dictionary[name];
                    item.LengthChanged -= OnLengthChanged;
                    Length -= GetBytesCountElement(name, _dictionary[name]);
                }

                item = value ?? BsonValue.Null;
                _dictionary[name] = item;

                if (item.IsDocument || item.IsArray)
                    item.LengthChanged += OnLengthChanged;

                Length += GetBytesCountElement(name, item);
            }
        }

        #region Length

        internal override int Length
        {
            get => _length;
            set
            {
                if (_length != value)
                {
                    var difference = value - _length;
                    _length = value;
                    NotifyLengthChanged(difference);
                }
            }
        }
        private int _length;

        private int GetBytesCountElement(string key, BsonValue value)
        {
            return
                1 + // element type
                Encoding.UTF8.GetByteCount(key) + // CString
                1 + // CString 0x00
                value.Length;
        }

        private void OnLengthChanged(object sender, int e) => Length += e;

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
                this[key] = other._dictionary[key];

            return this;
        }

        #endregion

        #region CompareTo / ToString

        public override int CompareTo(BsonValue other)
        {
            // if types are different, returns sort type order
            if (other.Type != BsonType.Document)
                return BsonType.Document.CompareTo(other.Type);

            int result;
            bool thisMove = true, otherMove = true;
            var otherDoc = other.AsDocument;

            using (var thisEnumerator = KeysOrdered.GetEnumerator())
            using (var otherEnumerator = otherDoc.KeysOrdered.GetEnumerator())
            {
                while (thisMove && otherMove)
                {
                    thisMove = thisEnumerator.MoveNext();
                    otherMove = otherEnumerator.MoveNext();

                    if (thisMove && otherMove)
                    {
                        result = this[thisEnumerator.Current].CompareTo(otherDoc[otherEnumerator.Current]);
                        if (result != 0)
                            return result;
                    }
                }

                if (thisMove)
                    return -1;

                if (otherMove)
                    return 1;

                return 0;
            }
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
            {
                var item = _dictionary[key];
                item.LengthChanged -= OnLengthChanged;
                Length -= GetBytesCountElement(key, item);
            }

            return _dictionary.Remove(key);
        }


        public bool TryGetValue(string key, out BsonValue value) => _dictionary.TryGetValue(key, out value);


        public void Add(KeyValuePair<string, BsonValue> item) => this[item.Key] = item.Value;


        public void Clear()
        {
            Length = 5;

            foreach (var item in _dictionary.Values)
                item.LengthChanged -= OnLengthChanged;

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
