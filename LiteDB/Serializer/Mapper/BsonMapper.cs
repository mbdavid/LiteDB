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
        private Dictionary<Type, Dictionary<string, PropertyMapper>> _mapper = new Dictionary<Type,Dictionary<string,PropertyMapper>>();

        /// <summary>
        /// Map serializer/deserialize for custom types
        /// </summary>
        private Dictionary<Type, Func<object, BsonValue>> _customSerializer = new Dictionary<Type, Func<object, BsonValue>>();
        private Dictionary<Type, Func<BsonValue, object>> _customDeserializer = new Dictionary<Type, Func<BsonValue, object>>();

        /// <summary>
        /// Map for autoId type based functions
        /// </summary>
        private Dictionary<Type, AutoId> _autoId = new Dictionary<Type, AutoId>();

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

            // register custom types
            this.RegisterType<Uri>
            (
                serialize: (uri) => uri.AbsoluteUri,
                deserialize: (bson) => new Uri(bson.AsString)
            );

            // register AutoId for ObjectId, Guid and Int32
            this.RegisterAutoId<ObjectId>
            (
                isEmpty: (v) => v.Equals(ObjectId.Empty),
                newId: (c) => ObjectId.NewObjectId()
            );

            this.RegisterAutoId<Guid>
            (
                isEmpty: (v) => v == Guid.Empty,
                newId: (c) => Guid.NewGuid()
            );

            this.RegisterAutoId<Int32>
            (
                isEmpty: (v) => v == 0, 
                newId: (c) => 
                { 
                    var max = c.Max(); 
                    return max.IsMaxValue ? 1 : (max + 1); 
                }
            );
        }

        /// <summary>
        /// Global BsonMapper instance
        /// </summary>
        public static BsonMapper Global = new BsonMapper();

        /// <summary>
        /// Register a custom type serializer/deserialize function
        /// </summary>
        public void RegisterType<T>(Func<T, BsonValue> serialize, Func<BsonValue, T> deserialize)
        {
            _customSerializer[typeof(T)] = (o) => serialize((T)o);
            _customDeserializer[typeof(T)] = (b) => (T)deserialize(b);
        }

        /// <summary>
        /// Register a custom Auto Id generator function for a type
        /// </summary>
        public void RegisterAutoId<T>(Func<T, bool> isEmpty, Func<LiteCollection<BsonDocument>, T> newId)
        {
            _autoId[typeof(T)] = new AutoId
            {
                IsEmpty = (o) => isEmpty((T)o),
                NewId = (c) => (T)newId(c)
            };
        }

        /// <summary>
        /// Set new Id in entity class
        /// </summary>
        public void SetAutoId(object entity, LiteCollection<BsonDocument> col)
        {
            // if object is BsonDocument, there is no AutoId
            if (entity is BsonDocument) return;

            // get fields mapper
            var mapper = this.GetPropertyMapper(entity.GetType());

            // it's not best way because is scan all properties - but Id propably is first field :)
            var id = mapper.Select(x => x.Value).FirstOrDefault(x => x.FieldName == "_id");

            // if not id or no autoId = true
            if (id == null || id.AutoId == false) return;

            AutoId autoId;

            if (_autoId.TryGetValue(id.PropertyType, out autoId))
            {
                var value = id.Getter(entity);

                if (value == null || autoId.IsEmpty(value) == true)
                {
                    var newId = autoId.NewId(col);

                    id.Setter(entity, newId);
                }
            }
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

        #endregion

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

        /// <summary>
        /// Search for [BsonIndex] in PropertyMapper. If not found, returns null
        /// </summary>
        internal IndexOptions GetIndexFromAttribute<T>(string field)
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
