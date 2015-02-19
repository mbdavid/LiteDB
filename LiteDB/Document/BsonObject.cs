using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LiteDB
{
    public class BsonObject : BsonValue
    {
        public BsonObject()
            : base(new Dictionary<string, BsonValue>())
        {
        }

        public BsonObject(int capacity)
            : base(new Dictionary<string, BsonValue>(capacity))
        {
        }

        public BsonObject(Dictionary<string, BsonValue> obj)
            : base(obj)
        {
        }

        public new Dictionary<string, BsonValue> RawValue
        {
            get
            {
                return (Dictionary<string, BsonValue>)base.RawValue;
            }
        }

        /// <summary>
        /// Add fields in fluent api
        /// </summary>
        public BsonObject Add(string key, BsonValue value)
        {
            this[key] = value;
            return this;
        }

        /// <summary>
        /// Returns all object keys
        /// </summary>
        public string[] Keys { get { return this.RawValue.Keys.ToArray(); } }

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
        /// Get value from a path - supports dotted name: Customer.Address.Street
        /// </summary>
        public object GetPathValue(string path)
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

                if (value[name].IsObject)
                {
                    value = value[name].AsObject;
                }
                else
                {
                    return null;
                }
            }

            return value[names.Last()].RawValue;
        }
    }
}
