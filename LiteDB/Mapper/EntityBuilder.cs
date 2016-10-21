using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    /// <summary>
    /// Helper class to modify your entity mapping to document. Can be used instead attribute decorates
    /// </summary>
    public class EntityBuilder<T>
    {
        private BsonMapper _mapper;
        private EntityMapper _entity;

        internal EntityBuilder(BsonMapper mapper)
        {
            _mapper = mapper;
            _entity = mapper.GetEntityMapper(typeof(T));
        }

        /// <summary>
        /// Define which property will not be mapped to document
        /// </summary>
        public EntityBuilder<T> Ignore<K>(Expression<Func<T, K>> property)
        {
            return this.GetProperty(property, (p) =>
            {
                _entity.Props.Remove(p);
            });
        }

        /// <summary>
        /// Define a custom name for a property when mapping to document
        /// </summary>
        public EntityBuilder<T> Field<K>(Expression<Func<T, K>> property, string field)
        {
            return this.GetProperty(property, (p) =>
            {
                p.FieldName = field;
            });
        }

        /// <summary>
        /// Define which property is your document id (primary key). Define if this property supports auto-id
        /// </summary>
        public EntityBuilder<T> Id<K>(Expression<Func<T, K>> property, bool autoId = true)
        {
            return this.GetProperty(property, (p) =>
            {
                p.FieldName = "_id";
                p.AutoId = autoId;
            });
        }

        /// <summary>
        /// Define an index based in a property on entity
        /// </summary>
        public EntityBuilder<T> Index<K>(Expression<Func<T, K>> property, bool unique = false)
        {
            return this.GetProperty(property, (p) =>
            {
                p.IndexInfo = unique;
            });
        }

        /// <summary>
        /// Define an index based in virtual property (getter function)
        /// </summary>
        public EntityBuilder<T> Index<K>(string indexName, Func<T, BsonValue> getter, bool unique = false)
        {
            _entity.Props.Add(new PropertyMapper
            {
                FieldName = indexName,
                PropertyName = indexName,
                Getter = x => (object)getter((T)x),
                Setter = null,
                PropertyType = typeof(BsonValue),
                IndexInfo = unique
            });

            return this;
        }

        /// <summary>
        /// Define an index based in a field name on BsonDocument
        /// </summary>
        public EntityBuilder<T> Index(string field, bool unique = false)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            var p = _entity.Props.FirstOrDefault(x => x.FieldName == field);

            if (p == null) throw new ArgumentException("field not found");

            p.IndexInfo = unique;

            return this;
        }

        #region DbRef

        /// <summary>
        /// Define a subdocument (or a list of) as a reference
        /// </summary>
        public EntityBuilder<T> DbRef<K>(Expression<Func<T, K>> property, string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName)) throw new ArgumentNullException("collectionName");

            return this.GetProperty(property, (p) =>
            {
                var typeRef = typeof(K);
                p.IsDbRef = true;

                if (Reflection.IsList(typeRef))
                {
                    var itemType = typeRef.IsArray ? typeRef.GetElementType() : Reflection.UnderlyingTypeOf(typeRef);
                    var entity = _mapper.GetEntityMapper(itemType);

                    RegisterDbRefList(p, collectionName, typeRef, entity);
                }
                else
                {
                    var entity = _mapper.GetEntityMapper(typeRef);

                    RegisterDbRef(p, collectionName, entity);
                }
            });
        }

        /// <summary>
        /// Register a property as a DbRef - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only
        /// </summary>
        internal static void RegisterDbRef(PropertyMapper p, string collectionName, EntityMapper itemEntity)
        {
            p.Serialize = (obj, m) =>
            {
                var idField = itemEntity.Id;

                var id = idField.Getter(obj);

                return new BsonDocument
                {
                    { "$id", new BsonValue(id) },
                    { "$ref", collectionName }
                };
            };

            p.Deserialize = (bson, m) =>
            {
                var idRef = bson.AsDocument["$id"];

                return m.Deserialize(itemEntity.ForType,
                    idRef.IsNull ?
                    bson : // if has no $id object was full loaded (via Include) - so deserialize using normal function
                    new BsonDocument { { "_id", idRef } }); // if has $id, deserialize object using only _id object
            };
        }

        /// <summary>
        /// Register a property as a DbRefList - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only
        /// </summary>
        internal static void RegisterDbRefList(PropertyMapper p, string collectionName, Type listType, EntityMapper itemEntity)
        {
            p.Serialize = (list, m) =>
            {
                var result = new BsonArray();
                var idField = itemEntity.Id;

                foreach (var item in (IEnumerable)list)
                {
                    result.Add(new BsonDocument
                    {
                        { "$id", new BsonValue(idField.Getter(item)) },
                        { "$ref", collectionName }
                    });
                }

                return result;
            };

            p.Deserialize = (bson, m) =>
            {
                var array = bson.AsArray;

                if (array.Count == 0) return m.Deserialize(listType, array);

                var hasIdRef = array[0].AsDocument["$id"].IsNull;

                if (hasIdRef)
                {
                    // if no $id, deserialize as full (was loaded via Include)
                    return m.Deserialize(listType, array);
                }
                else
                {
                    // copy array changing $id to _id
                    var arr = new BsonArray();

                    foreach (var item in array)
                    {
                        arr.Add(new BsonDocument { { "_id", item.AsDocument["$id"] } });
                    }

                    return m.Deserialize(listType, arr);
                }
            };
        }

        #endregion DbRef

        /// <summary>
        /// Get a property based on a expression. Eg.: 'x => x.UserId' return string "UserId"
        /// </summary>
        private EntityBuilder<T> GetProperty<TK, K>(Expression<Func<TK, K>> expr, Action<PropertyMapper> action)
        {
            if (expr == null) throw new ArgumentNullException("property");

            var prop = _entity.Props.FirstOrDefault(x => x.PropertyName == expr.Body.GetPath());

            if (prop == null) throw new ArgumentNullException(expr.GetPath());

            action(prop);

            return this;
        }
    }
}