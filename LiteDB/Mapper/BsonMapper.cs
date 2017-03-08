using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        #region Properties

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
        /// Type instantiator function to support IoC
        /// </summary>
        private readonly Func<Type, object> _typeInstantiator;

        /// <summary>
        /// Map for autoId type based functions
        /// </summary>
        private Dictionary<Type, AutoId> _autoId = new Dictionary<Type, AutoId>();

        /// <summary>
        /// Global instance used when no BsonMapper are passed in LiteDatabase ctor
        /// </summary>
        public static BsonMapper Global = new BsonMapper();

        /// <summary>
        /// A resolver name for field
        /// </summary>
        public Func<string, string> ResolveFieldName;

        /// <summary>
        /// Indicate that mapper do not serialize null values (default false)
        /// </summary>
        public bool SerializeNullValues { get; set; }

        /// <summary>
        /// Apply .Trim() in strings when serialize (default true)
        /// </summary>
        public bool TrimWhitespace { get; set; }

        /// <summary>
        /// Convert EmptyString to Null (default true)
        /// </summary>
        public bool EmptyStringToNull { get; set; }

        /// <summary>
        /// Get/Set that mapper must include fields (default: false)
        /// </summary>
        public bool IncludeFields { get; set; }

#if NET35
        /// <summary>
        /// Get/Set that mapper must include non public (private, protected and internal) (default: false)
        /// </summary>
        public bool IncludeNonPublic { get; set; }
#endif

        /// <summary>
        /// A custom callback to change MemberInfo behavior when converting to MemberMapper.
        /// Use mapper.ResolveMember(Type entity, MemberInfo property, MemberMapper documentMappedField)
        /// </summary>
        public Action<Type, MemberInfo, MemberMapper> ResolveMember;

        /// <summary>
        /// Custom resolve name collection based on Type 
        /// </summary>
        public Func<Type, string> ResolveCollectionName;

        #endregion

        public BsonMapper(Func<Type, object> customTypeInstantiator = null)
        {
            this.SerializeNullValues = false;
            this.TrimWhitespace = true;
            this.EmptyStringToNull = true;
            this.ResolveFieldName = (s) => s;
            this.ResolveMember = (t, mi, mm) => { };
            this.ResolveCollectionName = (t) => Reflection.IsList(t) ? Reflection.GetListItemType(t).Name : t.Name;

#if NET35
            this.IncludeFields = false;
#endif

            _typeInstantiator = customTypeInstantiator ?? Reflection.CreateInstance;

            #region Register CustomTypes

            RegisterType<Uri>(uri => uri.AbsoluteUri, bson => new Uri(bson.AsString));
            RegisterType<DateTimeOffset>(value => new BsonValue(value.UtcDateTime), bson => bson.AsDateTime.ToUniversalTime());
            RegisterType<TimeSpan>(value => new BsonValue(value.Ticks), bson => new TimeSpan(bson.AsInt64));

            #endregion Register CustomTypes

            #region Register AutoId

            // register AutoId for ObjectId, Guid and Int32
            RegisterAutoId
            (
                value => value.Equals(ObjectId.Empty),
                (db, col) => ObjectId.NewObjectId()
            );

            RegisterAutoId
            (
                value => value == Guid.Empty,
                (db, col) => Guid.NewGuid()
            );

            RegisterAutoId
            (
                value => value == 0,
                (db, col) =>
                {
                    var max = db.Max(col, "_id");
                    return max.IsMaxValue ? 1 : (max + 1);
                }
            );

            #endregion  

        }

        #region Register CustomType

        /// <summary>
        /// Register a custom type serializer/deserialize function
        /// </summary>
        public void RegisterType<T>(Func<T, BsonValue> serialize, Func<BsonValue, T> deserialize)
        {
            _customSerializer[typeof(T)] = (o) => serialize((T)o);
            _customDeserializer[typeof(T)] = (b) => (T)deserialize(b);
        }

        /// <summary>
        /// Register a custom type serializer/deserialize function
        /// </summary>
        public void RegisterType(Type type, Func<object, BsonValue> serialize, Func<BsonValue, object> deserialize)
        {
            _customSerializer[type] = (o) => serialize(o);
            _customDeserializer[type] = (b) => deserialize(b);
        }

        #endregion

        #region AutoId

        /// <summary>
        /// Register a custom Auto Id generator function for a type
        /// </summary>
        public void RegisterAutoId<T>(Func<T, bool> isEmpty, Func<LiteEngine, string, T> newId)
        {
            if (isEmpty == null) throw new ArgumentNullException("isEmpty");
            if (newId == null) throw new ArgumentNullException("newId");

            _autoId[typeof(T)] = new AutoId
            {
                IsEmpty = o => isEmpty((T)o),
                NewId = (db, col) => newId(db, col)
            };
        }

        /// <summary>
        /// Set new Id in entity class if entity needs one
        /// </summary>
        public virtual void SetAutoId(object entity, LiteEngine engine, string collection)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            if (engine == null) throw new ArgumentNullException("engine");
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException("collection");

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

            var id = mapper.Id;

            // if not id or no autoId = true
            if (id == null || id.AutoId == false) return;

            AutoId autoId;

            if (_autoId.TryGetValue(id.DataType, out autoId))
            {
                var value = id.Getter(entity);

                if (value == null || autoId.IsEmpty(value) == true)
                {
                    var newId = autoId.NewId(engine, collection);

                    id.Setter(entity, newId);
                }
            }
        }

        #endregion

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
            this.ResolveFieldName = (s) => char.ToLower(s[0]) + s.Substring(1);

            return this;
        }

        private Regex _lowerCaseDelimiter = new Regex("(?!(^[A-Z]))([A-Z])", RegexOptions.Compiled);

        /// <summary>
        /// Uses lower camel case with delimiter to convert property names to field names
        /// </summary>
        public BsonMapper UseLowerCaseDelimiter(char delimiter = '_')
        {
            this.ResolveFieldName = (s) => _lowerCaseDelimiter.Replace(s, delimiter + "$2").ToLower();

            return this;
        }

        #endregion

        #region GetEntityMapper

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
                        return _entities[type] = BuildEntityMapper(type);
                    }
                }
            }

            return mapper;
        }

        /// <summary>
        /// Use this method to override how your class can be, by default, mapped from entity to Bson document.
        /// Returns an EntityMapper from each requested Type
        /// </summary>
        protected virtual EntityMapper BuildEntityMapper(Type type)
        {
            var mapper = new EntityMapper
            {
                Members = new List<MemberMapper>(),
                ForType = type
            };

            var idAttr = typeof(BsonIdAttribute);
            var ignoreAttr = typeof(BsonIgnoreAttribute);
            var fieldAttr = typeof(BsonFieldAttribute);
            var indexAttr = typeof(BsonIndexAttribute);
            var dbrefAttr = typeof(BsonRefAttribute);
            var hasId = false;

            foreach (var memberInfo in this.GetTypeMembers(type))
            {
                // checks [BsonIgnore]
                if (memberInfo.IsDefined(ignoreAttr, true)) continue;

                // checks field name conversion
                var name = this.ResolveFieldName(memberInfo.Name);

                // checks if is _id
                if (hasId == false && this.IsMemberId(type, memberInfo))
                {
                    hasId = true;
                    name = "_id";
                }

                // check if property has [BsonField]
                var field = (BsonFieldAttribute)memberInfo.GetCustomAttributes(fieldAttr, false).FirstOrDefault();

                // check if property has [BsonField] with a custom field name
                if (field != null && field.Name != null)
                {
                    name = field.Name;
                }

                // test if field name is OK (avoid to check in all instances) - do not test internal classes, like DbRef
                if (BsonDocument.IsValidFieldName(name) == false) throw LiteException.InvalidFormat(memberInfo.Name, name);

                // create getter/setter function
                var getter = Reflection.CreateGenericGetter(type, memberInfo);
                var setter = Reflection.CreateGenericSetter(type, memberInfo);

                // check if property has [BsonId] to get with was setted AutoId = true
                var autoId = (BsonIdAttribute)memberInfo.GetCustomAttributes(idAttr, false).FirstOrDefault();

                // checks if this property has [BsonIndex]
                var index = (BsonIndexAttribute)memberInfo.GetCustomAttributes(indexAttr, false).FirstOrDefault();

                // get data type
                var dataType = memberInfo is PropertyInfo ?
                    (memberInfo as PropertyInfo).PropertyType :
                    (memberInfo as FieldInfo).FieldType;

                // check if datatype is list/array
                var isList = Reflection.IsList(dataType);

                // create a property mapper
                var member = new MemberMapper
                {
                    AutoId = autoId == null ? true : autoId.AutoId,
                    FieldName = name,
                    MemberName = memberInfo.Name,
                    DataType = dataType,
                    IsUnique = index == null ? false : index.Unique,
                    IsList = isList,
                    UnderlyingType = isList ? Reflection.GetListItemType(dataType) : dataType,
                    Getter = getter,
                    Setter = setter
                };

                // check if property has [BsonRef]
                var dbRef = (BsonRefAttribute)memberInfo.GetCustomAttributes(dbrefAttr, false).FirstOrDefault();

                if (dbRef != null && memberInfo is PropertyInfo)
                {
                    BsonMapper.RegisterDbRef(this, member, dbRef.Collection ?? this.ResolveCollectionName((memberInfo as PropertyInfo).PropertyType));
                }

                // support callback to user modify member mapper
                if (this.ResolveMember != null)
                {
                    this.ResolveMember(type, memberInfo, member);
                }

                // test if has name and there is no duplicate field
                if (member.FieldName != null && mapper.Members.Any(x => x.FieldName == name) == false)
                {
                    mapper.Members.Add(member);
                }
            }

            return mapper;
        }

        /// <summary>
        /// Checks if this member is Id field based on member name (Id) or [BsonId] attribute
        /// </summary>
        protected bool IsMemberId(Type type, MemberInfo member)
        {
            return member.IsDefined(typeof(BsonIdAttribute), false) ||
                member.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                member.Name.Equals(type.Name + "Id", StringComparison.OrdinalIgnoreCase) ||
                false;
        }

        /// <summary>
        /// Returns all member that will be have mapper between POCO class to document
        /// </summary>
        protected virtual IEnumerable<MemberInfo> GetTypeMembers(Type type)
        {
            var members = new List<MemberInfo>();

#if NET35
            var flags = this.IncludeNonPublic ?
                (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) :
                (BindingFlags.Public | BindingFlags.Instance);

            members.AddRange(type.GetProperties(flags).Where(x => x.CanRead).Select(x => x as MemberInfo));

            if(this.IncludeFields)
            {
                members.AddRange(type.GetFields(flags).Where(x => !x.Name.EndsWith("k__BackingField") && x.IsStatic == false).Select(x => x as MemberInfo));
            }
#else
            members.AddRange(type.GetRuntimeProperties().Where(x => x.CanRead).Select(x => x as MemberInfo));

            if(this.IncludeFields)
            {
                // in fields:
                // - remove when name ends with k__BackingField (Automatic Property syntax)
                // - remove static properties
                members.AddRange(type.GetRuntimeFields().Where(x => !x.Name.EndsWith("k__BackingField") && x.IsStatic == false));
            }
#endif

            return members;
        }

        #endregion

        #region Register DbRef

        /// <summary>
        /// Register a property mapper as DbRef to serialize/deserialize only document reference _id
        /// </summary>
        internal static void RegisterDbRef(BsonMapper mapper, MemberMapper member, string collection)
        {
            member.IsDbRef = true;

            if (member.IsList)
            {
                RegisterDbRefList(mapper, member, collection);
            }
            else
            {
                RegisterDbRefItem(mapper, member, collection);
            }
        }

        /// <summary>
        /// Register a property as a DbRef - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only
        /// </summary>
        private static void RegisterDbRefItem(BsonMapper mapper, MemberMapper member, string collection)
        {
            // get entity
            var entity = mapper.GetEntityMapper(member.DataType);

            member.Serialize = (obj, m) =>
            {
                var idField = entity.Id;

                var id = idField.Getter(obj);

                return new BsonDocument
                {
                    { "$id", new BsonValue(id) },
                    { "$ref", collection }
                };
            };

            member.Deserialize = (bson, m) =>
            {
                var idRef = bson.AsDocument["$id"];

                return m.Deserialize(entity.ForType,
                    idRef.IsNull ?
                    bson : // if has no $id object was full loaded (via Include) - so deserialize using normal function
                    new BsonDocument { { "_id", idRef } }); // if has $id, deserialize object using only _id object
            };
        }

        /// <summary>
        /// Register a property as a DbRefList - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only
        /// </summary>
        private static void RegisterDbRefList(BsonMapper mapper, MemberMapper member, string collection)
        {
            // get entity from list item type
            var entity = mapper.GetEntityMapper(member.UnderlyingType);

            member.Serialize = (list, m) =>
            {
                var result = new BsonArray();
                var idField = entity.Id;

                foreach (var item in (IEnumerable)list)
                {
                    if (item == null) continue;

                    result.Add(new BsonDocument
                    {
                        { "$id", new BsonValue(idField.Getter(item)) },
                        { "$ref", collection }
                    });
                }

                return result;
            };

            member.Deserialize = (bson, m) =>
            {
                var array = bson.AsArray;

                if (array.Count == 0) return m.Deserialize(member.DataType, array);

                var hasIdRef = array[0].AsDocument == null || array[0].AsDocument["$id"].IsNull;

                if (hasIdRef)
                {
                    // if no $id, deserialize as full (was loaded via Include)
                    return m.Deserialize(member.DataType, array);
                }
                else
                {
                    // copy array changing $id to _id
                    var arr = new BsonArray();

                    foreach (var item in array)
                    {
                        arr.Add(new BsonDocument { { "_id", item.AsDocument["$id"] } });
                    }

                    return m.Deserialize(member.DataType, arr);
                }
            };
        }

        #endregion
    }
}