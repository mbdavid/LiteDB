using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public class BsonDocument : BsonValue, IDictionary<string, BsonValue>
    {
        public BsonDocument()
            : base(new Dictionary<string, BsonValue>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public BsonDocument(ConcurrentDictionary<string, BsonValue> dict)
            : this(new Dictionary<string, BsonValue>(dict))
        {
        }

        public BsonDocument(Dictionary<string, BsonValue> dict)
            : base(dict)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
        }

        /// <summary>
        /// Get/Set position of this document inside database. It's filled when used in Find operation.
        /// </summary>
        internal PageAddress RawId { get; set; } = PageAddress.Empty;

        /// <summary>
        /// Get/Set a field for document. Fields are case sensitive
        /// </summary>
        public override BsonValue this[string name]
        {
            get => this.DocValue.GetOrDefault(name, BsonValue.Null);
            set => this.DocValue[name] = value ?? BsonValue.Null;
        }

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
            foreach (var key in other.DocValue.Keys)
            {
                this.DocValue[key] = other.DocValue[key];
            }

            return this;
        }

        #endregion

        #region CompareTo / ToString

        public override int CompareTo(BsonValue other)
        {
            // if types are different, returns sort type order
            if (other.Type != BsonType.Document)
                return this.Type.CompareTo(other.Type);

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

        public ICollection<string> Keys => this.DocValue.Keys.OrderBy(x => x == "_id" ? 1 : 2).ToList();


        public ICollection<BsonValue> Values => this.DocValue.Values;


        public int Count => this.DocValue.Count;


        public bool IsReadOnly => false;


        public bool ContainsKey(string key) => this.DocValue.ContainsKey(key);


        public void Add(string key, BsonValue value) => this[key] = value;


        public bool Remove(string key) => this.DocValue.Remove(key);


        public bool TryGetValue(string key, out BsonValue value) => this.DocValue.TryGetValue(key, out value);


        public void Add(KeyValuePair<string, BsonValue> item) => this[item.Key] = item.Value;


        public void Clear() => this.DocValue.Clear();


        public bool Contains(KeyValuePair<string, BsonValue> item) => this.DocValue.Contains(item);


        public void CopyTo(KeyValuePair<string, BsonValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, BsonValue>>)this.DocValue).CopyTo(array, arrayIndex);


        public void CopyTo(BsonDocument doc)
        {
            foreach (var key in this.DocValue.Keys)
                doc.DocValue[key] = this.DocValue[key];
        }

        public bool Remove(KeyValuePair<string, BsonValue> item) => this.DocValue.Remove(item.Key);


        public IEnumerator<KeyValuePair<string, BsonValue>> GetEnumerator() => this.DocValue.GetEnumerator();


        IEnumerator IEnumerable.GetEnumerator() => this.DocValue.GetEnumerator();

        #endregion
    }
}
