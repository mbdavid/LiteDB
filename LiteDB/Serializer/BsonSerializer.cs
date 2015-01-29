using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// This class contains only static method for serialize/deserialize objects
    /// and Get/Set informations on poco objects or BsonDocument
    /// </summary>
    internal class BsonSerializer
    {
        static BsonSerializer()
        {
            fastBinaryJSON.BJSON.Parameters.UseExtensions = false;
            fastBinaryJSON.BJSON.Parameters.IgnoreAttributes.Clear();

            // BsonId will can be excluded from byte[] data on convert - DataBlock as a special Key data
            fastBinaryJSON.BJSON.Parameters.IgnoreAttributes.Add(typeof(BsonIdAttribute));
            fastBinaryJSON.BJSON.Parameters.IgnoreAttributes.Add(typeof(BsonIgnoreAttribute));

            fastBinaryJSON.BJSON.Parameters.UsingGlobalTypes = false;
        }

        public static byte[] Serialize(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");

            var bytes = obj is BsonDocument ?
                fastBinaryJSON.BJSON.ToBJSON(((BsonDocument)obj).RawValue) :
                fastBinaryJSON.BJSON.ToBJSON(obj);

            if (bytes.Length > BsonDocument.MAX_DOCUMENT_SIZE)
                throw new LiteException("Document size too long");

            return bytes;
        }

        public static T Deserialize<T>(IndexKey key, byte[] data)
        {
            if (data == null || data.Length == 0) throw new ArgumentNullException("data");

            object doc;

            if (typeof(T) == typeof(BsonDocument))
            {
                var dict = fastBinaryJSON.BJSON.Parse(data);

                doc = new BsonDocument((Dictionary<string, object>)dict);
            }
            else
            {
                doc = fastBinaryJSON.BJSON.ToObject<T>(data);
            }

            SetIdValue(doc, key.Value);

            return (T)doc;
        }

        /// <summary>
        /// Gets from a document object (plain C# object or BsonDocument) some field value
        /// </summary>
        public static object GetFieldValue(object obj, string fieldName)
        {
            // supports parent.child.name
            var names = fieldName.Split('.');

            if (obj is BsonDocument)
            {
                var value = (BsonValue)obj;

                if (names.Length == 1)
                {
                    return value[fieldName].RawValue;
                }

                foreach (var name in names)
                {
                    if (!value.IsObject) return null;
                    value = value[name];
                }

                return value.RawValue;
            }
            else
            {
                if (names.Length == 1)
                {
                    var info = obj.GetType().GetProperty(fieldName);
                    return info == null ? null : info.GetValue(obj, null);
                }

                foreach (var name in names)
                {
                    if (obj == null) return null;
                    var info = obj.GetType().GetProperty(name);
                    if (info == null) return null;
                    obj = info.GetValue(obj, null);
                }

                return obj;
            }
        }

        private static Dictionary<Type, PropertyInfo> _cacheId = new Dictionary<Type, PropertyInfo>();

        /// <summary>
        /// Get Id value from a document object (plain C# object or BsonDocument) 
        /// </summary>
        public static object GetIdValue(object obj)
        {
            return obj is BsonDocument ?
                ((BsonDocument)obj).Id :
                GetIdProperty(obj.GetType()).GetValue(obj, null);
        }

        /// <summary>
        /// Set Id value to document object (plain C# object or BsonDocument) 
        /// </summary>
        public static void SetIdValue(object obj, object id)
        {
            if(obj is BsonDocument)
            {
                ((BsonDocument)obj).Id = id;
            }
            else
            {
                GetIdProperty(obj.GetType()).SetValue(obj, id, null);
            }
        }

        /// <summary>
        /// Gets PropertyInfo that refers to Id from a document object.
        /// </summary>
        public static PropertyInfo GetIdProperty(Type type)
        {
            if (_cacheId.ContainsKey(type))
                return _cacheId[type];

            // Get all properties and test in order: BsonIdAttribute, "Id" name, "<typeName>Id" name
            var prop = SelectProperty(type.GetProperties(),
                x => Attribute.IsDefined(x, typeof(BsonIdAttribute), true));
                //x => x.Name.Equals("Id", StringComparison.InvariantCultureIgnoreCase),
                //x => x.Name.Equals(type.Name + "Id", StringComparison.InvariantCultureIgnoreCase));

            if (prop != null)
            {
                lock (_cacheId)
                {
                    _cacheId[type] = prop;
                }
                return prop;
            }

            // if not found, throw an exception
            throw new LiteException("Id property not found in object " + type.Name + ". Use [BsonId] attribute or 'Id' property name");
        }

        private static PropertyInfo SelectProperty(IEnumerable<PropertyInfo> props, params Func<PropertyInfo, bool>[] predicates)
        {
            foreach (var predicate in predicates)
            {
                var prop = props.FirstOrDefault(predicate);

                if (prop != null)
                {
                    if (!prop.CanRead || !prop.CanWrite)
                        throw new LiteException(prop.Name + " property must have get; set;");

                    return prop;
                }
            }

            return null;
        }

        /// <summary>
        /// Convert a string to a specific type. Has more convert options from Convert.ChangeType (like Guid)
        /// </summary>
        public static object ChangeType(object value, Type type)
        {
            if (value == null) return null;

            if (type == typeof(Guid))
            {
                var tvalue = value.GetType();

                if (tvalue == typeof(Guid)) return value;
                if (tvalue == typeof(string)) return new Guid((string)value);
                if (tvalue == typeof(byte[])) return new Guid((byte[])value);
                throw new ArgumentException("Guid error convert type");
            }

            return Convert.ChangeType(value, type);
        }
    }
}
