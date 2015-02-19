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
        /// Serialize a POCO class to BsonDocument
        /// </summary>
        public BsonDocument ToDocument(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");

            // if object is BsonDocument, just return them
            if (obj is BsonDocument) return (BsonDocument)obj;

            return this.Serialize(obj, 0).AsDocument;
        }

        private BsonValue Serialize(object obj, int depth)
        {
            if (++depth > MAX_DEPTH) throw new LiteException("Serialization class reach MAX_DEPTH - Check for circular references");

            if (obj == null) return BsonValue.Null;

            var type = obj.GetType();

            // basic Bson data types
            if (obj is String || obj is Int32 || obj is Int64 || obj is Double || obj is Boolean || obj is Byte[] || obj is DateTime || obj is Guid)
            {
                return new BsonValue(obj);
            }
            // basic .net type to convert to bson
            else if (obj is Int16 || obj is UInt16 || obj is Byte)
            {
                return new BsonValue(Convert.ToInt32(obj));
            }
            else if (obj is UInt32 || obj is UInt64)
            {
                return new BsonValue(Convert.ToInt64(obj));
            }
            else if (obj is Single || obj is Decimal)
            {
                return new BsonValue(Convert.ToDouble(obj));
            }
            else if (obj is Char || obj is Enum)
            {
                return new BsonValue(obj.ToString());
            }
            // check if is a list or array
            else if (obj is IList || type.IsArray)
            {
                return this.SerializeArray(obj as IList, depth);
            }
            // for dictionary
            else if (obj is IDictionary)
            {
                return this.SerializeDictionary(obj as IDictionary, depth);
            }
            // otherwise treat as a plain object
            else
            {
                return this.SerializeObject(type, obj, depth);
            }
        }

        private BsonArray SerializeArray(IEnumerable array, int depth)
        {
            var arr = new BsonArray();

            foreach (var item in array)
            {
                arr.Add(this.Serialize(item, depth));
            }

            return arr;
        }

        private BsonObject SerializeDictionary(IDictionary dict, int depth)
        {
            var o = new BsonObject();

            foreach (var key in dict.Keys)
            {
                var value = dict[key];
                o.Add(key.ToString(), this.Serialize(value, depth));
            }

            return o;
        }

        private BsonObject SerializeObject(Type type, object obj, int depth)
        {
            var o = new BsonObject();
            var mapper = this.GetPropertyMapper(type);

            foreach (var prop in mapper.Values)
            {
                // get property value 
                var value = prop.Getter(obj);

                if (value == null && this.SerializeNullValues == false) continue;

                o.Add(prop.ResolvedName, this.Serialize(value, depth));
            }

            return o;
        }
    }
}
