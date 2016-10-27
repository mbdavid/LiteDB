using System;
using System.Collections.Generic;
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
        private Dictionary<Type, EntityMapper> _entities = new Dictionary<Type, EntityMapper>();

        /// <summary>
        /// Map serializer/deserialize for custom types
        /// </summary>
        private Dictionary<Type, Func<object, BsonValue>> _customSerializer = new Dictionary<Type, Func<object, BsonValue>>();

        private Dictionary<Type, Func<BsonValue, object>> _customDeserializer = new Dictionary<Type, Func<BsonValue, object>>();

        /// <summary>
        /// Get type initializator to support IoC
        /// </summary>
        private readonly Func<Type, object> _typeInstanciator;

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

        /// <summary>
        /// Map for autoId type based functions
        /// </summary>
        private Dictionary<Type, AutoId> _autoId = new Dictionary<Type, AutoId>();

        /// <summary>
        /// Global instance used when no BsonMapper are passed in LiteDatabase ctor
        /// </summary>
        public static BsonMapper Global = new BsonMapper();

        public BsonMapper(Func<Type, object> customTypeInstanciator = null)
        {
            this.SerializeNullValues = false;
            this.TrimWhitespace = true;
            this.EmptyStringToNull = true;
            this.ResolvePropertyName = (s) => s;

            _typeInstanciator = customTypeInstanciator ?? Reflection.CreateInstance;

            #region Register CustomTypes

            RegisterType<Uri>(uri => uri.AbsoluteUri, bson => new Uri(bson.AsString));
            RegisterType<DateTimeOffset>(value => new BsonValue(value.UtcDateTime), bson => bson.AsDateTime.ToUniversalTime());
            RegisterType<TimeSpan>(value => new BsonValue(value.Ticks), bson => new TimeSpan(bson.AsInt64));

            #endregion Register CustomTypes

            #region Register AutoId

            // register AutoId for ObjectId, Guid and Int32
            RegisterAutoId
            (
                v => v.Equals(ObjectId.Empty),
                c => ObjectId.NewObjectId()
            );

            RegisterAutoId
            (
                v => v == Guid.Empty,
                c => Guid.NewGuid()
            );

            RegisterAutoId
            (
                v => v == 0,
                c =>
                {
                    var max = c.Max();
                    return max.IsMaxValue ? 1 : (max + 1);
                }
            );

            #endregion  

        }

        /// <summary>
        /// Register a custom type serializer/deserialize function
        /// </summary>
        public virtual void RegisterType<T>(Func<T, BsonValue> serialize, Func<BsonValue, T> deserialize)
        {
            _customSerializer[typeof(T)] = (o) => serialize((T)o);
            _customDeserializer[typeof(T)] = (b) => (T)deserialize(b);
        }

        /// <summary>
        /// Register a custom type serializer/deserialize function
        /// </summary>
        public virtual void RegisterType(Type type, Func<object, BsonValue> serialize, Func<BsonValue, object> deserialize)
        {
            _customSerializer[type] = (o) => serialize(o);
            _customDeserializer[type] = (b) => deserialize(b);
        }

        /// <summary>
        /// Register a custom Auto Id generator function for a type
        /// </summary>
        public virtual void RegisterAutoId<T>(Func<T, bool> isEmpty, Func<LiteCollection<BsonDocument>, T> newId)
        {
            _autoId[typeof(T)] = new AutoId
            {
                IsEmpty = o => isEmpty((T)o),
                NewId = c => newId(c)
            };
        }

        /// <summary>
        /// Set new Id in entity class if entity needs one
        /// </summary>
        public virtual void SetAutoId(object entity, LiteCollection<BsonDocument> col)
        {
            // if object is BsonDocument, add _id as ObjectId
            if (entity is BsonDocument)
            {
                var doc = entity as BsonDocument;
                if (!doc.RawValue.ContainsKey("_id"))
                {
                    doc["_id"] = ObjectId.NewObjectId();
                }
                return;
            }

            // get fields mapper
            var mapper = this.GetEntityMapper(entity.GetType());

            // if not id or no autoId = true
            if (mapper.Id == null || mapper.Id.AutoId == false) return;

            AutoId autoId;

            if (_autoId.TryGetValue(mapper.Id.PropertyType, out autoId))
            {
                var value = mapper.Id.Getter(entity);

                if (value == null || autoId.IsEmpty(value) == true)
                {
                    var newId = autoId.NewId(col);

                    mapper.Id.Setter(entity, newId);
                }
            }
        }

        /// <summary>
        /// Map your entity class to BsonDocument using fluent API
        /// </summary>
        public EntityBuilder<T> Entity<T>()
        {
            return new EntityBuilder<T>(this);
        }

        #region Predefinded Property Resolvers

        /// <summary>
        /// Use lower camel case resolution for convert property names to field names
        /// </summary>
        public BsonMapper UseCamelCase()
        {
            this.ResolvePropertyName = (s) => char.ToLower(s[0]) + s.Substring(1);

            return this;
        }

        private Regex _lowerCaseDelimiter = new Regex("(?!(^[A-Z]))([A-Z])");

        /// <summary>
        /// Use lower camel case with delemiter resolution for convert property names to field names
        /// </summary>
        public BsonMapper UseLowerCaseDelimiter(char delimiter = '_')
        {
            this.ResolvePropertyName = (s) => _lowerCaseDelimiter.Replace(s, delimiter + "$2").ToLower();

            return this;
        }

        #endregion

        /// <summary>
        /// Get property mapper between typed .NET class and BsonDocument - Cache results
        /// </summary>
        internal EntityMapper GetEntityMapper(Type type)
        {
            //TODO: needs check if Type if BsonDocument? Returns empty EntityMapper?
            EntityMapper mapper;

            if (!_entities.TryGetValue(type, out mapper))
            {
                lock (_entities)
                {
                    if (!_entities.TryGetValue(type, out mapper))
                    {
                        return _entities[type] = new EntityMapper(type, this.ResolvePropertyName);
                    }
                }
            }

            return mapper;
        }
    }
}