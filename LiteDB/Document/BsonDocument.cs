using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public class BsonDocument : BsonValue
    {
        public const int MAX_DOCUMENT_SIZE = 256 * BasePage.PAGE_AVAILABLE_BYTES; // limits in 1.044.224b max document size to avoid large documents, memory usage and slow performance

        public BsonDocument()
            : base(new Dictionary<string, BsonValue>())
        {
        }

        public BsonDocument(Dictionary<string, BsonValue> dict)
            : base(dict)
        {
            if (dict == null) throw new ArgumentNullException("dict");
        }

        public new Dictionary<string, BsonValue> RawValue
        {
            get
            {
                return (Dictionary<string, BsonValue>)base.RawValue;
            }
        }

        /// <summary>
        /// Get/Set a field for document. Fields are case sensitive
        /// </summary>
        public BsonValue this[string name]
        {
            get
            {
                return this.RawValue.GetOrDefault(name, BsonValue.Null);
            }
            set
            {
                if (!IsValidFieldName(name)) throw new ArgumentException(string.Format("Field '{0}' has an invalid name.", name));

                this.RawValue[name] = value ?? BsonValue.Null;
            }
        }

        #region Methods

        /// <summary>
        /// Add fields in fluent api
        /// </summary>
        public BsonDocument Add(string key, BsonValue value)
        {
            this[key] = value;
            return this;
        }

        /// <summary>
        /// Returns all object keys with _id in first order
        /// </summary>
        public IEnumerable<string> Keys
        {
            get
            {
                var keys = this.RawValue.Keys;

                if (keys.Contains("_id")) yield return "_id";

                foreach (var key in keys.Where(x => x != "_id"))
                {
                    yield return key;
                }
            }
        }

        /// <summary>
        /// Returns how many fields this object contains
        /// </summary>
        public int Count
        {
            get
            {
                return this.RawValue.Count;
            }
        }

        /// <summary>
        /// Returns if object contains a named property
        /// </summary>
        public bool ContainsKey(string name)
        {
            return this.RawValue.ContainsKey(name);
        }

        /// <summary>
        /// Remove a specific key on object
        /// </summary>
        public bool RemoveKey(string key)
        {
            return this.RawValue.Remove(key);
        }

        /// <summary>
        /// Test if field name is a valid string: only [\w$]+(\w-$)*
        /// </summary>
        internal static bool IsValidFieldName(string field)
        {
            if (string.IsNullOrEmpty(field)) return false;

            // do not use regex because is too slow
            for (var i = 0; i < field.Length; i++)
            {
                var c = field[i];

                if (char.IsLetterOrDigit(c) || c == '_' || c == '$')
                {
                    continue;
                }
                else if (c == '-' && i > 0)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        #endregion Methods

        #region Get/Set methods

        /// <summary>
        /// Get value from a path - supports dotted path like: Customer.Address.Street
        /// </summary>
        public BsonValue Get(string path)
        {
            // supports parent.child.name
            var names = path.Split('.');

            if (names.Length == 1)
            {
                return this[path];
            }

            var value = this;

            for (var i = 0; i < names.Length - 1; i++)
            {
                var name = names[i];

                if (value[name].IsDocument)
                {
                    value = value[name].AsDocument;
                }
                else
                {
                    return BsonValue.Null;
                }
            }

            return value[names.Last()];
        }

        /// <summary>
        /// Set value to a path - supports dotted path like: Customer.Address.Street - Fluent API (returns same BsonDocument)
        /// </summary>
        public BsonDocument Set(string path, BsonValue value)
        {
            // supports parent.child.name
            var names = path.Split('.');

            if (names.Length == 1)
            {
                this[path] = value;
                return this;
            }

            var doc = this;

            // walk on path creating object when do not exists
            for (var i = 0; i < names.Length - 1; i++)
            {
                var name = names[i];

                if (doc[name].IsDocument)
                {
                    doc = doc[name].AsDocument;
                }
                else if (doc[name].IsNull)
                {
                    var d = new BsonDocument();
                    doc[name] = d;
                    doc = d;
                }
                else
                {
                    return this;
                }
            }

            doc[names.Last()] = value;

            return this;
        }

        #endregion Get/Set methods

        #region CompareTo / ToString

        public override int CompareTo(BsonValue other)
        {
            // if types are diferent, returns sort type order
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

            // are diferents
            if (result != 0) return result;

            // test keys length to check wich is bigger
            if (i == thisLength) return i == otherLength ? 0 : -1;
            return 1;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, false, true);
        }

        #endregion CompareTo / ToString
    }
}