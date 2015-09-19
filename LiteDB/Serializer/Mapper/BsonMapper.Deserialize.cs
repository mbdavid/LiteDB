using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public partial class BsonMapper
    {
        /// <summary>
        /// Deserialize a BsonDocument to entity class
        /// </summary>
        public T ToObject<T>(BsonDocument doc)
            where T : new()
        {
            if (doc == null) throw new ArgumentNullException("doc");

            var type = typeof(T);

            // if T is BsonDocument, just return them
            if (type == typeof(BsonDocument)) return (T)(object)doc;

            return (T)this.Deserialize(type, doc);
        }

        /// <summary>
        /// Deserialize an BsonValue to .NET object typed in T
        /// </summary>
        public T Deserialize<T>(BsonValue value)
        {
            if (value == null) return default(T);

            var result = this.Deserialize(typeof(T), value);

            return (T)result;
        }

        #region Basic direct .NET convert types

        // direct bson types
        private HashSet<Type> _bsonTypes = new HashSet<Type>
        {
            typeof(String),
            typeof(Int32),
            typeof(Int64),
            typeof(Boolean),
            typeof(Guid),
            typeof(DateTime),
            typeof(Byte[]),
            typeof(ObjectId),
            typeof(Double)
        };

        // simple convert types
        private HashSet<Type> _basicTypes = new HashSet<Type>
        {
            typeof(Int16),
            typeof(UInt16),
            typeof(UInt32),
            typeof(UInt64),
            typeof(Single),
            typeof(Decimal),
            typeof(Char),
            typeof(Byte)
        };

        #endregion

        private object Deserialize(Type type, BsonValue value)
        {
            Func<BsonValue, object> custom;

            // null value - null returns
            if (value.IsNull) return null;

            // if is nullable, get underlying type
            else if (Reflection.IsNullable(type))
            {
                type = Reflection.UnderlyingTypeOf(type);
            }

            // check if your type is already a BsonValue/BsonDocument/BsonArray
            if (type == typeof(BsonValue))
            {
                return new BsonValue(value);
            }
            else if (type == typeof(BsonDocument))
            {
                return value.AsDocument;
            }
            else if (type == typeof(BsonArray))
            {
                return value.AsArray;
            }

            // raw values to native bson values
            else if (_bsonTypes.Contains(type))
            {
                return value.RawValue;
            }

            // simple ConvertTo to basic .NET types
            else if (_basicTypes.Contains(type))
            {
                return Convert.ChangeType(value.RawValue, type);
            }

            // enum value is a string
            else if (type.IsEnum)
            {
                return Enum.Parse(type, value.AsString);
            }

            // test if has a custom type implementation
            else if (_customDeserializer.TryGetValue(type, out custom))
            {
                return custom(value);
            }

            // if value is array, deserialize as array
            else if (value.IsArray)
            {
                if (type.IsArray)
                {
                    return this.DeserializeArray(type.GetElementType(), value.AsArray);
                }
                else
                {
                    return this.DeserializeList(type, value.AsArray);
                }
            }

            // if value is document, deserialize as document
            else if (value.IsDocument)
            {
                BsonValue typeField;
                var doc = value.AsDocument;

                // test if value is object and has _type
                if (doc.RawValue.TryGetValue("_type", out typeField))
                {
                    type = Type.GetType(typeField.AsString);
                }

                var o = Reflection.CreateInstance(type);

                if (o is IDictionary && type.IsGenericType)
                {
                    var k = type.GetGenericArguments()[0];
                    var t = type.GetGenericArguments()[1];

                    this.DeserializeDictionary(k, t, (IDictionary)o, value.AsDocument);
                }
                else
                {
                    this.DeserializeObject(type, o, doc);
                }

                return o;
            }

            // in last case, return value as-is - can cause "cast error"
            // it's used for "public object MyInt { get; set; }"
            return value.RawValue;
        }

        private object DeserializeArray(Type type, BsonArray array)
        {
            var arr = Array.CreateInstance(type, array.Count);
            var idx = 0;

            foreach (var item in array)
            {
                arr.SetValue(this.Deserialize(type, item), idx++);
            }

            return arr;
        }

        private object DeserializeList(Type type, BsonArray value)
        {
            var itemType = Reflection.UnderlyingTypeOf(type);
            var list = (IList)Reflection.CreateInstance(type);

            foreach (var item in value)
            {
                list.Add(this.Deserialize(itemType, item));
            }

            return list;
        }

        private void DeserializeDictionary(Type K, Type T, IDictionary dict, BsonDocument value)
        {
            foreach (var key in value.Keys)
            {
                var k = Convert.ChangeType(key, K);
                var v = this.Deserialize(T, value[key]);

                dict.Add(k, v);
            }
        }

        private void DeserializeObject(Type type, object obj, BsonDocument value)
        {
            var props = this.GetPropertyMapper(type);

            foreach (var prop in props.Values)
            {
                // property is read only
                if (prop.Setter == null) continue;

                var val = value[prop.FieldName];

                if (!val.IsNull)
                {
                    prop.Setter(obj, this.Deserialize(prop.PropertyType, val));
                }
            }
        }
    }
}
