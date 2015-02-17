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
    /// <summary>
    /// Class that converts POCO class to/from BsonDocument
    /// If you prefer use a new instance of BsonMapper (not Global), be sure cache this instance for better performance 
    /// Serialization rules:
    ///     - Classes must be "public"
    ///     - Properties must have public getter and setter
    ///     - Entity class must have Id property, [ClassName]Id property or [BsonId] attribute
    ///     - No circular references
    ///     - Fields are not valid
    /// </summary>
    public class BsonMapper
    {
        private const int MAX_DEPTH = 20;

        /// <summary>
        /// Mapping cache between Class/BsonDocument
        /// </summary>
        private Dictionary<Type, Dictionary<string, PropertyMapper>> _mapper = new Dictionary<Type,Dictionary<string,PropertyMapper>>();

        /// <summary>
        /// A resolver name property
        /// </summary>
        public Func<string, string> ResolvePropertyName;

        /// <summary>
        /// Indicate that mapper do not serialize null values
        /// </summary>
        public bool SerializeNullValues { get; set; }

        public BsonMapper()
        {
            this.SerializeNullValues = false;
            this.ResolvePropertyName = (s) => s;
        }

        #region Static global instance - used for cache data

        public static BsonMapper Global { get; private set; }

        static BsonMapper()
        {
            Global = new BsonMapper();
        }

        #endregion

        #region Predefinded Property Resolvers

        public void UseCamelCase()
        {
            this.ResolvePropertyName = (s) => char.ToLower(s[0]) + s.Substring(1);
        }

        private Regex _lowerCaseDelimiter = new Regex("(?!(^[A-Z]))([A-Z])");

        public void UseLowerCaseDelimiter(char delimiter = '_')
        {
            this.ResolvePropertyName = (s) => _lowerCaseDelimiter.Replace(s, delimiter + "$2").ToLower();
        }

        #endregion

        #region Mapper methods

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

        /// <summary>
        /// Deserialize a BsonDocument to POCO class
        /// </summary>
        public T ToObject<T>(BsonDocument doc)
            where T : new()
        {
            var type = typeof(T);

            // if T is BsonDocument, just return them
            if (type == typeof(BsonDocument)) return (T)(object)doc;

            return (T)this.Deserialize(type, doc);
        }

        /// <summary>
        /// Get property mapper between typed .NET class and BsonDocument - Cache results
        /// </summary>
        internal Dictionary<string, PropertyMapper> GetPropertyMapper(Type type)
        {
            Dictionary<string, PropertyMapper> props;

            if (_mapper.TryGetValue(type, out props))
            {
                return props;
            }

            _mapper[type] = Reflection.GetProperties(type, this.ResolvePropertyName);

            return _mapper[type];
        }

        #endregion

        #region Serialize

        private BsonValue Serialize(object obj, int depth)
        {
            if (depth > MAX_DEPTH) throw new LiteException("Serialization class reach MAX_DEPTH - Check for circular references");

            if (obj == null) return BsonValue.Null;

            var type = obj.GetType();

            // basic Bson data types
            if (obj is String || obj is Int32 || obj is Int64 || obj is Double || obj is Boolean || obj is Byte[] || obj is DateTime || obj is Guid)
            {
                return new BsonValue(obj);
            }
            // basic .net type to convert to bson
            else if (obj is Int16 || obj is UInt16)
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
            // check if is a list
            else if (obj is IList)
            {
                return this.SerializeArray(obj as IList, depth);
            }
            //else if (obj is IDictionary)
            //{
            //}

            // do more...

            // last case: a plain object
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
                arr.Add(this.Serialize(item, depth + 1));
            }

            return arr;
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

                o.Add(prop.ResolvedName, this.Serialize(value, depth + 1));
            }

            return o;
        }
        #endregion

        #region Deserialize

        private object Deserialize(Type type, BsonValue value)
        {
            if (value.IsNull) return null;

            if (Reflection.IsNullable(type))
            {
                type = Reflection.UnderlyingTypeOf(type);
            }

            // bson types convert
            if (type == typeof(String) || 
                type == typeof(Int32) || 
                type == typeof(Int64) || 
                type == typeof(Boolean) || 
                type == typeof(Guid) || 
                type == typeof(DateTime) || 
                type == typeof(Byte[]) || 
                type == typeof(Double))
                return value.RawValue;

            var o = Reflection.CreateInstance(type);

            // check if type is a IList
            if (o is IList && type.IsGenericType)
            {
                var typeT = Reflection.UnderlyingTypeOf(type);
                var array = value.AsArray;
                var list = (IList)o;

                foreach (var item in array)
                {
                    list.Add(this.Deserialize(typeT, item));
                }

                return o;
            }

            // otherwise is a object
            var obj = value.AsObject;
            var props = this.GetPropertyMapper(type);

            foreach (var prop in props.Values)
            {
                var val = obj[prop.ResolvedName];

                if (!val.IsNull)
                {
                    prop.Setter(o, this.Deserialize(prop.PropertyType, val));
                }
            }

            return o;
        }

        #endregion

    }
}
