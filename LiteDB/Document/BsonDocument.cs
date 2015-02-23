using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

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

        public BsonValue this[string name]
        {
            get
            {
                return this.RawValue.GetOrDefault(name, BsonValue.Null);
            }
            set
            {
                if (!this.IsValidFieldName(name)) throw new ArgumentException(string.Format("Field name '{0}' is invalid pattern or reserved keyword", name));

                this.RawValue[name] = value ?? BsonValue.Null;
            }
        }

        #region Mapper/Bson/Json

        #endregion

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
        /// Returns all object keys
        /// </summary>
        public string[] Keys { get { return this.RawValue.Keys.ToArray(); } }

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
            return this.ContainsKey(name);
        }

        /// <summary>
        /// Check if this object has a specific key
        /// </summary>
        public bool HasKey(string key)
        {
            return this.RawValue.ContainsKey(key);
        }

        /// <summary>
        /// Remove a specific key on object
        /// </summary>
        public bool RemoveKey(string key)
        {
            return this.RawValue.Remove(key);
        }

        /// <summary>
        /// Test if field name is a valid string: only $-_[a-z][A-Z]
        /// </summary>
        private bool IsValidFieldName(string field)
        {
            // test if keywords
            if (field == "$date" || field == "$guid" || field == "$numberLong" || field == "$binary")
            {
                return false;
            }

            // do not use regex because is too slow
            for (var i = 0; i < field.Length; i++)
            {
                var c = field[i];

                if(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '$')
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

        #endregion

        #region Get/Set methods

        /// <summary>
        /// Get value from a path - supports dotted path like: Customer.Address.Street
        /// </summary>
        public object Get(string path)
        {
            // supports parent.child.name
            var names = path.Split('.');

            if (names.Length == 1)
            {
                return this[path].RawValue;
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
                    return null;
                }
            }

            return value[names.Last()].RawValue;
        }

        /// <summary>
        /// Set value to a path - supports dotted path like: Customer.Address.Street - Returns new BsonValue
        /// </summary>
        public BsonValue Set(string path, BsonValue value)
        {
            // supports parent.child.name
            var names = path.Split('.');

            if (names.Length == 1)
            {
                this[path] = value;
                return value;
            }

            //var obj = this[names[0]];

            //for (var i = 1; i < names.Length - 1; i++)
            //{
            //    if(obj.IsObject)
            //    {
            //        obj = 
            //    }
            //    else if(obj.IsNull)
            //    {
            //        this[names
            //    }
            //    var val = value[name];
            //    if (obj.IsObject)
            //    {
            //        value = obj.AsObject;
            //    }
            //    else if(obj.is
            //    {
            //        obj = new BsonObject();

            //    }
            //}

            //return value[names.Last()].RawValue;


            return value;
        }

        #endregion
    }
}
