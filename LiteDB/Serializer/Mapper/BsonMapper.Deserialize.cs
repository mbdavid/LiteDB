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
            if (value.IsNull) return null;

            // if is nullable, get underlying type
            if (Reflection.IsNullable(type))
            {
                type = Reflection.UnderlyingTypeOf(type);
            }

            // check if your type is already a BsonValue
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

            // bson types convert
            else if (_bsonTypes.Contains(type))
            {
                return value.RawValue;
            }
            else if (_basicTypes.Contains(type))
            {
                return Convert.ChangeType(value.RawValue, type);
            }
            else if (type.IsEnum)
            {
                return Enum.Parse(type, value.AsString);
            }

            // test if has a custom type implementation
            Func<BsonValue, object> custom;

            if (_customDeserializer.TryGetValue(type, out custom))
            {
                return custom(value);
            }

            // if value is an array
            if (value.IsArray)
            {
                // and if Type is an array
                if (type.IsArray)
                {
                    return this.DeserializeArray(type.GetElementType(), value.AsArray);
                }
                // if type is IList<>
                //else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    return this.DeserializeList(type, value.AsArray);
                }

                throw new NotSupportedException("BsonMapper `" + type.Name + "` not supported for array");
            }

            // for last case, value is a document
            else if(value.IsDocument)
            {
                var doc = value.AsDocument;

                // if type is a dictionary, deserialize as a dict
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return this.DeserializeDictionary(type, doc);
                }

                return this.DeserializeObject(type, doc);
            }

            throw new NotSupportedException("Type " + type.Name + " not supported on BsonMapper");
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
            var listType = Reflection.GetGenericListOfType(type);
            var list = (IList)Reflection.CreateInstance(listType);

            foreach (var item in value)
            {
                list.Add(this.Deserialize(itemType, item));
            }

            return list;
        }

        private object DeserializeDictionary(Type type, BsonDocument value)
        {
            var K = type.GetGenericArguments()[0];
            var V = type.GetGenericArguments()[1];
            var dictType = Reflection.GetGenericDictionaryOfType(K, V);
            var dict = (IDictionary)Reflection.CreateInstance(dictType);

            foreach (var key in value.Keys)
            {
                var k = Convert.ChangeType(key, K);
                var v = this.Deserialize(V, value[key]);

                dict.Add(k, v);
            }

            return dict;
        }

        private object DeserializeObject(Type type, BsonDocument value)
        {
            // if there is a _type in object, use the to create instance
            BsonValue typeField;

            if (value.RawValue.TryGetValue("_type", out typeField))
            {
                type = Type.GetType(typeField.AsString);
            }

            var obj = Reflection.CreateInstance(type);
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

            return obj;
        }
    }
}
