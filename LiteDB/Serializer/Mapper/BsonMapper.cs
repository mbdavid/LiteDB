using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Class that converts your entity class to/from BsonDocument
    /// If you prefer use a new instance of BsonMapper (not Global), be sure cache this instance for better performance
    /// Serialization rules:
    ///     - Classes must be "public" with a public constructor (without parameters)
    ///     - Properties must have public getter (can be read-only)
    ///     - Entity class must have Id property, [ClassName]Id property or [BsonId] attribute
    ///     - No circular references
    ///     - Fields are not valid
    ///     - IList, Array supports
    ///     - IDictionary supports (Key must be a simple datatype - converted by ChangeType)
    /// </summary>
    public partial class BsonMapper
    {
        private const int MAX_DEPTH = 20;

        /// <summary>
        /// Mapping cache between Class/BsonDocument
        /// </summary>
        private Dictionary<Type, Dictionary<string, PropertyMapper>> _mapper = new Dictionary<Type, Dictionary<string, PropertyMapper>>();

        /// <summary>
        /// Map serializer/deserialize for custom types
        /// </summary>
        private Dictionary<Type, Func<object, BsonValue>> _customSerializer = new Dictionary<Type, Func<object, BsonValue>>();

        private Dictionary<Type, Func<BsonValue, object>> _customDeserializer = new Dictionary<Type, Func<BsonValue, object>>();

        /// <summary>
        /// A resolver name property
        /// </summary>
        public Func<string, string> ResolvePropertyName;

        /// <summary>
        /// Indicate that mapper do not serialize null values
        /// </summary>
        public bool SerializeNullValues { get; set; }

        /// <summary>
        /// Apply .Trim() in strings
        /// </summary>
        public bool TrimWhitespace { get; set; }

        /// <summary>
        /// Convert EmptyString to Null
        /// </summary>
        public bool EmptyStringToNull { get; set; }

        public BsonMapper()
        {
            this.SerializeNullValues = false;
            this.TrimWhitespace = true;
            this.EmptyStringToNull = true;
            this.ResolvePropertyName = (s) => s;

            #region Register CustomTypes

            // register custom types
            this.RegisterType<Uri>
            (
                serialize: (uri) => uri.AbsoluteUri,
                deserialize: (bson) => new Uri(bson.AsString)
            );

            this.RegisterType<NameValueCollection>
            (
                serialize: (nv) =>
                {
                    var doc = new BsonDocument();

                    foreach (var key in nv.AllKeys)
                    {
                        doc[key] = nv[key];
                    }

                    return doc;
                },
                deserialize: (bson) =>
                {
                    var nv = new NameValueCollection();
                    var doc = bson.AsDocument;

                    foreach (var key in doc.Keys)
                    {
                        nv[key] = doc[key].AsString;
                    }

                    return nv;
                }
            );

            #endregion Register CustomTypes
        }

        /// <summary>
        /// Register a custom type serializer/deserialize function
        /// </summary>
        public void RegisterType<T>(Func<T, BsonValue> serialize, Func<BsonValue, T> deserialize)
        {
            _customSerializer[typeof(T)] = (o) => serialize((T)o);
            _customDeserializer[typeof(T)] = (b) => (T)deserialize(b);
        }

        /// <summary>
        /// Map your entity class to BsonDocument using fluent API
        /// </summary>
        public EntityBuilder<T> Entity<T>()
        {
            return new EntityBuilder<T>(this);
        }

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

        #endregion Predefinded Property Resolvers

        /// <summary>
        /// Get property mapper between typed .NET class and BsonDocument - Cache results
        /// </summary>
        internal Dictionary<string, PropertyMapper> GetPropertyMapper(Type type)
        {
            Dictionary<string, PropertyMapper> props;

            if (!_mapper.TryGetValue(type, out props))
            {
                lock (_mapper)
                {
                    if (!_mapper.TryGetValue(type, out props))
                    {
                        return _mapper[type] = Reflection.GetProperties(type, this.ResolvePropertyName);
                    }
                }
            }

            return props;
        }

        /// <summary>
        /// Search for [BsonIndex]/Entity.Index() in PropertyMapper. If not found, returns null
        /// </summary>
        internal IndexOptions GetIndexFromMapper<T>(string field)
        {
            var props = this.GetPropertyMapper(typeof(T));

            // get index options if type has
            return props.Values
                .Where(x => x.FieldName == field && x.IndexOptions != null)
                .Select(x => x.IndexOptions)
                .FirstOrDefault();
        }
    }
}