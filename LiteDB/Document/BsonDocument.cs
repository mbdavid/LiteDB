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

        public new Dictionary<string, BsonValue> RawValue
        {
            get
            {
                return (Dictionary<string, BsonValue>)base.RawValue;
            }
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
            get
            {
                return this.RawValue.GetOrDefault(name, BsonValue.Null);
            }
            set
            {
                this.Length = null; // reset GetBytesLength()

                this.RawValue[name] = value ?? BsonValue.Null;
            }
        }

        #region Update support with expressions

        /// <summary>
        /// Get an IEnumerable of values from a json-like path inside document. Use BsonExpression to parse this path
        /// </summary>
        public IEnumerable<BsonValue> Get(string pathExpression)
        {
            var expr = BsonExpression.Create(pathExpression);

            return expr.Execute(this);
        }

        /// <summary>
        /// Copy all properties from other document inside this current document
        /// </summary>
        public BsonDocument Extend(BsonDocument other)
        {
            var myDict = this.RawValue;
            var otherDict = other.RawValue;

            foreach (var key in other.RawValue.Keys)
            {
                myDict[key] = otherDict[key];
            }

            this.Length = null; // reset GetBytesLength()

            return this;
        }

        #endregion

        #region CompareTo / ToString

        public override int CompareTo(BsonValue other)
        {
            // if types are different, returns sort type order
            if (other.Type != BsonType.Document) return this.Type.CompareTo(other.Type);

            var thisKeys = this.Keys.ToArray();
            var thisLength = thisKeys.Length;

            var otherDoc = other.AsDocument;
            var otherKeys = otherDoc.Keys.ToArray();
            var otherLength = otherKeys.Length;

            var result = 0;
            var i = 0;
            var stop = Math.Min(thisLength, otherLength);

            for (; 0 == result && i < stop; i++)
                result = this[thisKeys[i]].CompareTo(otherDoc[thisKeys[i]]);

            // are different
            if (result != 0) return result;

            // test keys length to check which is bigger
            if (i == thisLength) return i == otherLength ? 0 : -1;

            return 1;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        #endregion

        #region IDictionary

        public ICollection<string> Keys => this.RawValue.Keys;

        public ICollection<BsonValue> Values => this.RawValue.Values;

        public int Count => this.RawValue.Count;

        public bool IsReadOnly => false;

        public bool ContainsKey(string key) => this.RawValue.ContainsKey(key);

        /// <summary>
        /// Get all document elements - Return "_id" as first of all (if exists)
        /// </summary>
        public IEnumerable<KeyValuePair<string, BsonValue>> GetElements()
        {
            if(this.RawValue.TryGetValue("_id", out var id))
            {
                yield return new KeyValuePair<string, BsonValue>("_id", id);
            }

            foreach(var item in this.RawValue.Where(x => x.Key != "_id"))
            {
                yield return item;
            }
        }

        public void Add(string key, BsonValue value)
        {
            this.Length = null; // reset GetBytesLength()

            this[key] = value;
        }

        public bool Remove(string key)
        {
            this.Length = null; // reset GetBytesLength()

            return this.RawValue.Remove(key);
        }

        public bool TryGetValue(string key, out BsonValue value) => this.RawValue.TryGetValue(key, out value);

        public void Add(KeyValuePair<string, BsonValue> item)
        {
            this.Length = null; // reset GetBytesLength()

            this[item.Key] = item.Value;
        }

        public void Clear()
        {
            this.Length = null; // reset GetBytesLength()

            this.RawValue.Clear();
        }

        public bool Contains(KeyValuePair<string, BsonValue> item) => this.RawValue.Contains(item);

        public void CopyTo(KeyValuePair<string, BsonValue>[] array, int arrayIndex)
        {
            this.Length = null; // reset GetBytesLength()

            ((ICollection<KeyValuePair<string, BsonValue>>)this.RawValue).CopyTo(array, arrayIndex);
        }

        public void CopyTo(BsonDocument doc)
        {
            var myDict = this.RawValue;
            var otherDict = doc.RawValue;

            foreach(var key in myDict.Keys)
            {
                otherDict[key] = myDict[key];
            }
        }

        public bool Remove(KeyValuePair<string, BsonValue> item)
        {
            this.Length = null; // reset GetBytesLength()

            return this.RawValue.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, BsonValue>> GetEnumerator() => this.RawValue.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.RawValue.GetEnumerator();

        #endregion
    }
}
