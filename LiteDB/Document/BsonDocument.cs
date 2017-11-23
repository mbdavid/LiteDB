using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public class BsonDocument : BsonValue, IDictionary<string, BsonValue>
    {
        public BsonDocument()
            : base(new Dictionary<string, BsonValue>())
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

        /// <summary>
        /// Test if field name is a valid string: only [\w$]+[\w-]*
        /// </summary>
        internal static bool IsValidFieldName(string field)
        {
            if (string.IsNullOrEmpty(field)) return false;

            // do not use regex because is too slow
            for (var i = 0; i < field.Length; i++)
            {
                var c = field[i];

                if (char.IsLetterOrDigit(c) || c == '_' || (c == '$' && i == 0))
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

        #region Update support with expressions

        /// <summary>
        /// Get an IEnumerable of values from a json-like path inside document. Use BsonExpression to parse this path
        /// </summary>
        public IEnumerable<BsonValue> Get(string path, bool includeNullIfEmpty = false)
        {
            var expr = new BsonExpression(new StringScanner(path), true, true);

            return expr.Execute(this, includeNullIfEmpty);
        }

        /// <summary>
        /// Find the field inside document tree, using json-like path, and update with an expression paramter. If field nod exists, create new field. Return true if document was changed
        /// </summary>
        public bool Set(string path, BsonExpression expr)
        {
            if (expr == null) throw new ArgumentNullException(nameof(expr));

            var value = expr.Execute(this, true).First();

            return this.Set(path, value);
        }

        /// <summary>
        /// Find the field inside document tree, using json-like path, and update with value paramter. If field nod exists, create new field. Return true if document was changed
        /// </summary>
        public bool Set(string path, BsonValue value)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(value));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var field = path.StartsWith("$") ? path : "$." + path;
            var parent = field.Substring(0, field.LastIndexOf('.'));
            var key = field.Substring(field.LastIndexOf('.') + 1);
            var expr = new BsonExpression(parent);
            var changed = false;

            foreach (var item in expr.Execute(this, false).Where(x => x.IsDocument))
            {
                var idoc = item.AsDocument;
                var cur = idoc[key];

                // update field only if value are different from current value
                if (cur != value)
                {
                    idoc[key] = value;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Set or add a value to document using a json-like path to update/create this field
        /// </summary>
        public bool Set(string path, BsonExpression expr, bool addInArray)
        {
            if (expr == null) throw new ArgumentNullException(nameof(expr));

            var value = expr.Execute(this, true).First();

            return this.Set(path, value, addInArray);
        }

        /// <summary>
        /// Set or add a value to document using a json-like path to update/create this field. If you addInArray, only add if path returns an array.
        /// </summary>
        public bool Set(string path, BsonValue value, bool addInArray)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(value));
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (addInArray == false) return this.Set(path, value);

            var expr = new BsonExpression(path.StartsWith("$") ? path : "$." + path);
            var changed = false;

            foreach (var arr in expr.Execute(this, false).Where(x => x.IsArray))
            {
                arr.AsArray.Add(value);
                changed = true;
            }

            return changed;
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
            return JsonSerializer.Serialize(this, false, true);
        }

        #endregion

        #region IDictionary

        public ICollection<string> Keys
        {
            get
            {
                return this.RawValue.Keys
                    .OrderBy(x => x == "_id" ? 1 : 2)
                    .ToList();
            }
        }

        public ICollection<BsonValue> Values
        {
            get
            {
                return this.RawValue.Values;
            }
        }

        public int Count
        {
            get
            {
                return this.RawValue.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool ContainsKey(string key)
        {
            return this.RawValue.ContainsKey(key);
        }

        public void Add(string key, BsonValue value)
        {
            this[key] = value;
        }

        public bool Remove(string key)
        {
            return this.RawValue.Remove(key);
        }

        public bool TryGetValue(string key, out BsonValue value)
        {
            return this.RawValue.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, BsonValue> item)
        {
            this[item.Key] = item.Value;
        }

        public void Clear()
        {
            this.RawValue.Clear();
        }

        public bool Contains(KeyValuePair<string, BsonValue> item)
        {
            return this.RawValue.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, BsonValue>[] array, int arrayIndex)
        {
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
            return this.RawValue.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, BsonValue>> GetEnumerator()
        {
            return this.RawValue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.RawValue.GetEnumerator();
        }

        #endregion
    }
}
