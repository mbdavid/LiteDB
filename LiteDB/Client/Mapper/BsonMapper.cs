using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

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
        /// Type name binder to control how type names are serialized to BSON documents
        /// </summary>
        private readonly ITypeNameBinder _typeNameBinder;

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
        /// Get/Set if enum must be converted into Integer value. If false, enum will be converted into String value.
        /// MUST BE "true" to support LINQ expressions (default false)
        /// </summary>
        public bool EnumAsInteger { get; set; }

        /// <summary>
        /// Get/Set that mapper must include fields (default: false)
        /// </summary>
        public bool IncludeFields { get; set; }

        /// <summary>
        /// Get/Set that mapper must include non public (private, protected and internal) (default: false)
        /// </summary>
        public bool IncludeNonPublic { get; set; }

        /// <summary>
        /// A custom callback to change MemberInfo behavior when converting to MemberMapper.
        /// Use mapper.ResolveMember(Type entity, MemberInfo property, MemberMapper documentMappedField)
        /// Set FieldName to null if you want remove from mapped document
        /// </summary>
        public Action<Type, MemberInfo, MemberMapper> ResolveMember;

        /// <summary>
        /// Custom resolve name collection based on Type 
        /// </summary>
        public Func<Type, string> ResolveCollectionName;

        #endregion

        public BsonMapper(Func<Type, object> customTypeInstantiator = null, ITypeNameBinder typeNameBinder = null)
        {
            this.SerializeNullValues = false;
            this.TrimWhitespace = true;
            this.EmptyStringToNull = true;
            this.EnumAsInteger = false;
            this.ResolveFieldName = (s) => s;
            this.ResolveMember = (t, mi, mm) => { };
            this.ResolveCollectionName = (t) => Reflection.IsList(t) ? Reflection.GetListItemType(t).Name : t.Name;
            this.IncludeFields = false;

            _typeInstantiator = customTypeInstantiator ?? ((Type t) => null);
            _typeNameBinder = typeNameBinder ?? DefaultTypeNameBinder.Instance;

            #region Register CustomTypes

            RegisterType<Uri>(uri => uri.AbsoluteUri, bson => new Uri(bson.AsString));
            RegisterType<DateTimeOffset>(value => new BsonValue(value.UtcDateTime), bson => bson.AsDateTime.ToUniversalTime());
            RegisterType<TimeSpan>(value => new BsonValue(value.Ticks), bson => new TimeSpan(bson.AsInt64));
            RegisterType<Regex>(
                r => r.Options == RegexOptions.None ? new BsonValue(r.ToString()) : new BsonDocument { { "p", r.ToString() }, { "o", (int)r.Options } },
                value => value.IsString ? new Regex(value) : new Regex(value.AsDocument["p"].AsString, (RegexOptions)value.AsDocument["o"].AsInt32)
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

        /// <summary>
        /// Map your entity class to BsonDocument using fluent API
        /// </summary>
        public EntityBuilder<T> Entity<T>()
        {
            return new EntityBuilder<T>(this, _typeNameBinder);
        }

        #region Get LinqVisitor processor

        /// <summary>
        /// Resolve LINQ expression into BsonExpression
        /// </summary>
        public BsonExpression GetExpression<T, K>(Expression<Func<T, K>> predicate)
        {
            var visitor = new LinqExpressionVisitor(this, predicate);

            var expr = visitor.Resolve(typeof(K) == typeof(bool));

            LOG($"`{predicate.ToString()}` -> `{expr.Source}`", "LINQ");

            return expr;
        }

        #endregion

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

            if (!_entities.TryGetValue(type, out EntityMapper mapper))
            {
                lock (_entities)
                {
                    if (!_entities.TryGetValue(type, out mapper))
                    {
                        return _entities[type] = this.BuildEntityMapper(type);
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
            var mapper = new EntityMapper(type);

            var idAttr = typeof(BsonIdAttribute);
            var ignoreAttr = typeof(BsonIgnoreAttribute);
            var fieldAttr = typeof(BsonFieldAttribute);
            var dbrefAttr = typeof(BsonRefAttribute);

            var members = this.GetTypeMembers(type);
            var id = this.GetIdMember(members);

            foreach (var memberInfo in members)
            {
                // checks [BsonIgnore]
                if (memberInfo.IsDefined(ignoreAttr, true)) continue;

                // checks field name conversion
                var name = this.ResolveFieldName(memberInfo.Name);

                // check if property has [BsonField]
                var field = (BsonFieldAttribute)memberInfo.GetCustomAttributes(fieldAttr, false).FirstOrDefault();

                // check if property has [BsonField] with a custom field name
                if (field != null && field.Name != null)
                {
                    name = field.Name;
                }

                // checks if memberInfo is id field
                if (memberInfo == id)
                {
                    name = "_id";
                }

                // create getter/setter function
                var getter = Reflection.CreateGenericGetter(type, memberInfo);
                var setter = Reflection.CreateGenericSetter(type, memberInfo);

                // check if property has [BsonId] to get with was setted AutoId = true
                var autoId = (BsonIdAttribute)memberInfo.GetCustomAttributes(idAttr, false).FirstOrDefault();

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
                    IsList = isList,
                    UnderlyingType = isList ? Reflection.GetListItemType(dataType) : dataType,
                    Getter = getter,
                    Setter = setter
                };

                // check if property has [BsonRef]
                var dbRef = (BsonRefAttribute)memberInfo.GetCustomAttributes(dbrefAttr, false).FirstOrDefault();

                if (dbRef != null && memberInfo is PropertyInfo)
                {
                    BsonMapper.RegisterDbRef(this, member, _typeNameBinder, dbRef.Collection ?? this.ResolveCollectionName((memberInfo as PropertyInfo).PropertyType));
                }

                // support callback to user modify member mapper
                this.ResolveMember?.Invoke(type, memberInfo, member);

                // test if has name and there is no duplicate field
                if (member.FieldName != null && mapper.Members.Any(x => x.FieldName.Equals(name, StringComparison.InvariantCultureIgnoreCase)) == false)
                {
                    mapper.Members.Add(member);
                }
            }

            return mapper;
        }

        /// <summary>
        /// Gets MemberInfo that refers to Id from a document object.
        /// </summary>
        protected virtual MemberInfo GetIdMember(IEnumerable<MemberInfo> members)
        {
            return Reflection.SelectMember(members,
                x => Attribute.IsDefined(x, typeof(BsonIdAttribute), true),
                x => x.Name.Equals("Id", StringComparison.OrdinalIgnoreCase),
                x => x.Name.Equals(x.DeclaringType.Name + "Id", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns all member that will be have mapper between POCO class to document
        /// </summary>
        protected virtual IEnumerable<MemberInfo> GetTypeMembers(Type type)
        {
            var members = new List<MemberInfo>();

            var flags = this.IncludeNonPublic ?
                (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) :
                (BindingFlags.Public | BindingFlags.Instance);

            members.AddRange(type.GetProperties(flags)
                .Where(x => x.CanRead && x.GetIndexParameters().Length == 0)
                .Select(x => x as MemberInfo));

            if(this.IncludeFields)
            {
                members.AddRange(type.GetFields(flags).Where(x => !x.Name.EndsWith("k__BackingField") && x.IsStatic == false).Select(x => x as MemberInfo));
            }

            return members;
        }

        /// <summary>
        /// Get best construtor to use to initialize this entity.
        /// - Look if contains [BsonCtor] attribute
        /// - Look for parameterless ctor
        /// - Look for first contructor with parameter and use BsonDocument to send RawValue
        /// </summary>
        protected virtual CreateObject GetTypeCtor(EntityMapper mapper)
        {
            var ctors = mapper.ForType.GetConstructors();

            var ctor = 
                ctors.FirstOrDefault(x => x.GetCustomAttribute<BsonCtorAttribute>() != null && x.GetParameters().All(p => Reflection.ConvertType.ContainsKey(p.ParameterType))) ??
                ctors.FirstOrDefault(x => x.GetParameters().Length == 0) ??
                ctors.FirstOrDefault(x => x.GetParameters().All(p => Reflection.ConvertType.ContainsKey(p.ParameterType)));

            if (ctor == null) return null;

            var pars = new List<Expression>();
            var pDoc = Expression.Parameter(typeof(BsonDocument), "_doc");

            // otherwise, need access ctor with parameter
            foreach (var p in ctor.GetParameters())
            {
                // try first get converted named (useful for Id => _id)
                var name = mapper.Members.FirstOrDefault(x => x.MemberName.Equals(p.Name, StringComparison.OrdinalIgnoreCase))?.FieldName ??
                    p.Name;

                var expr = Expression.MakeIndex(pDoc,
                    Reflection.DocumentItemProperty,
                    new[] { Expression.Constant(name) });

                var prop = Expression.Property(expr, Reflection.ConvertType[p.ParameterType]);

                pars.Add(prop);
            }

            // get `new MyClass([params])` expression
            var newExpr = Expression.New(ctor, pars.ToArray());

            // get lambda expression
            var fn = mapper.ForType.IsClass ? 
                Expression.Lambda<CreateObject>(newExpr, pDoc).Compile() : // Class
                Expression.Lambda<CreateObject>(Expression.Convert(newExpr, typeof(object)), pDoc).Compile(); // Struct

            return fn;
        }

        #endregion

        #region Register DbRef

        /// <summary>
        /// Register a property mapper as DbRef to serialize/deserialize only document reference _id
        /// </summary>
        internal static void RegisterDbRef(BsonMapper mapper, MemberMapper member, ITypeNameBinder typeNameBinder, string collection)
        {
            member.IsDbRef = true;

            if (member.IsList)
            {
                RegisterDbRefList(mapper, member, typeNameBinder, collection);
            }
            else
            {
                RegisterDbRefItem(mapper, member, typeNameBinder, collection);
            }
        }

        /// <summary>
        /// Register a property as a DbRef - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only
        /// </summary>
        private static void RegisterDbRefItem(BsonMapper mapper, MemberMapper member, ITypeNameBinder typeNameBinder, string collection)
        {
            // get entity
            var entity = mapper.GetEntityMapper(member.DataType);

            member.Serialize = (obj, m) =>
            {
                // supports null values when "SerializeNullValues = true"
                if (obj == null) return BsonValue.Null;

                var idField = entity.Id;

                // #768 if using DbRef with interface with no ID mapped
                if (idField == null) throw new LiteException(0, "There is no _id field mapped in your type: " + member.DataType.FullName);

                var id = idField.Getter(obj);

                var bsonDocument = new BsonDocument
                {
                    ["$id"] = m.Serialize(id.GetType(), id, 0),
                    ["$ref"] = collection
                };

                if (member.DataType != obj.GetType())
                {
                    bsonDocument["$type"] = typeNameBinder.GetName(obj.GetType());
                }

                return bsonDocument;
            };

            member.Deserialize = (bson, m) =>
            {
                var idRef = bson.IsDocument ? bson["$id"] : BsonValue.Null;
                var missing = bson.IsDocument ? (bson["$missing"] == true) : false;
                
                if (missing) return null;

                return m.Deserialize(entity.ForType,
                    idRef.IsNull ?
                    bson : // if has no $id object was full loaded (via Include) - so deserialize using normal function
                    bson.AsDocument.ContainsKey("$type") ?
                        new BsonDocument { ["_id"] = idRef, ["_type"] = bson["$type"] } :
                        new BsonDocument { ["_id"] = idRef }); // if has $id, deserialize object using only _id object
            };
        }

        /// <summary>
        /// Register a property as a DbRefList - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only
        /// </summary>
        private static void RegisterDbRefList(BsonMapper mapper, MemberMapper member, ITypeNameBinder typeNameBinder, string collection)
        {
            // get entity from list item type
            var entity = mapper.GetEntityMapper(member.UnderlyingType);

            member.Serialize = (list, m) =>
            {
                // supports null values when "SerializeNullValues = true"
                if (list == null) return BsonValue.Null;

                var result = new BsonArray();
                var idField = entity.Id;

                foreach (var item in (IEnumerable)list)
                {
                    if (item == null) continue;

                    var id = idField.Getter(item);

                    var bsonDocument = new BsonDocument
                    {
                        ["$id"] = m.Serialize(id.GetType(), id, 0),
                        ["$ref"] = collection
                    };

                    if (member.UnderlyingType != item.GetType())
                    {
                        bsonDocument["$type"] = typeNameBinder.GetName(item.GetType());
                    }

                    result.Add(bsonDocument);
                }

                return result;
            };

            member.Deserialize = (bson, m) =>
            {
                if (bson.IsArray == false) return null;

                var array = bson.AsArray;

                if (array.Count == 0) return m.Deserialize(member.DataType, array);

                // copy array changing $id to _id
                var result = new BsonArray();

                foreach (var item in array)
                {
                    var refId = item["$id"];
                    var missing = item["$missing"] == true;
                    
                    // if referece document are missing, do not inlcude on output list
                    if (missing) continue;

                    // if refId is null was included by "include" query, so "item" is full filled document
                    if (refId.IsNull)
                    {
                        result.Add(item);
                    }
                    else
                    {
                        var bsonDocument = new BsonDocument { ["_id"] = refId };

                        if (item.AsDocument.ContainsKey("$type"))
                        {
                            bsonDocument["_type"] = item["$type"];
                        }

                        result.Add(bsonDocument);
                    }

                }

                return m.Deserialize(member.DataType, result);
            };
        }

        #endregion
    }
}