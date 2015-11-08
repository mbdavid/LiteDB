using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Helper class to modify your entity mapping to document. Can be used instead attribute decorates
    /// </summary>
    public class EntityBuilder<T>
    {
        private BsonMapper _mapper;
        private Dictionary<string, PropertyMapper> _prop;

        internal EntityBuilder(BsonMapper mapper)
        {
            _mapper = mapper;
            _prop = mapper.GetPropertyMapper(typeof(T));
        }

        /// <summary>
        /// Define which property will not be mapped to document
        /// </summary>
        public EntityBuilder<T> Ignore<K>(Expression<Func<T, K>> property)
        {
            return this.GetProperty(property, (p) =>
            {
                _prop.Remove(p.PropertyName);
            });
        }

        /// <summary>
        /// Define a custom name for a property when mapping to document
        /// </summary>
        public EntityBuilder<T> Map<K>(Expression<Func<T, K>> property, string field)
        {
            return this.GetProperty(property, (p) =>
            {
                p.FieldName = field;
            });
        }

        /// <summary>
        /// Define which property is your document id (primary key). Define if this property supports auto-id
        /// </summary>
        public EntityBuilder<T> Key<K>(Expression<Func<T, K>> property, bool autoId = true)
        {
            return this.GetProperty(property, (p) =>
            {
                p.FieldName = "_id";
                p.AutoId = autoId;
            });
        }

        /// <summary>
        /// Define an index based in a field on entity
        /// </summary>
        public EntityBuilder<T> Index<K>(Expression<Func<T, K>> property, bool unique = false)
        {
            return this.GetProperty(property, (p) =>
            {
                p.IndexOptions = new IndexOptions { Unique = unique };
            });
        }

        /// <summary>
        /// Define an index based in a field on entity
        /// </summary>
        public EntityBuilder<T> Index<K>(Expression<Func<T, K>> property, IndexOptions options)
        {
            return this.GetProperty(property, (p) =>
            {
                p.IndexOptions = options;
            });
        }

        #region DbRef

        /// <summary>
        /// Define a subdocument (or a list of) as a reference
        /// </summary>
        public EntityBuilder<T> DbRef<K>(Expression<Func<T, K>> property, string collectionName)
        {
            return this.GetProperty(property, (p) =>
            {
                var typeK = typeof(K);

                p.CollectionRef = collectionName;

                if (Reflection.IsList(typeK))
                {
                    var itemType = typeK.IsArray ? typeK.GetElementType() : Reflection.UnderlyingTypeOf(typeK);
                    var mapper = _mapper.GetPropertyMapper(itemType);

                    RegisterDbRefList(p, collectionName, typeK, itemType, mapper);
                }
                else
                {
                    var mapper = _mapper.GetPropertyMapper(typeK);

                    RegisterDbRef(p, collectionName, typeK, mapper);
                }
            });
        }

        /// <summary>
        /// Register a property as a DbRef - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only 
        /// </summary>
        internal static void RegisterDbRef(PropertyMapper p, string collectionName, Type itemType, Dictionary<string, PropertyMapper> itemMapper)
        {
            p.Serialize = (obj) =>
            {
                var idField = itemMapper.Select(x => x.Value).FirstOrDefault(x => x.FieldName == "_id");

                var id = idField.Getter(obj);

                return new BsonDocument()
                    .Add("$id", new BsonValue(id))
                    .Add("$ref", collectionName);
            };

            p.Deserialize = (bson) =>
            {
                var idField = itemMapper.Select(x => x.Value).FirstOrDefault(x => x.FieldName == "_id");

                var instance = Reflection.CreateInstance(itemType);

                idField.Setter(instance, bson.AsDocument["$id"].RawValue);

                return instance;
            };
        }

        /// <summary>
        /// Register a property as a DbRefList - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only 
        /// </summary>
        internal static void RegisterDbRefList(PropertyMapper p, string collectionName, Type listType, Type itemType, Dictionary<string, PropertyMapper> itemMapper)
        {
            p.Serialize = (list) =>
            {
                var result = new BsonArray();
                var idField = itemMapper.Select(x => x.Value).FirstOrDefault(x => x.FieldName == "_id");

                foreach (var item in (IEnumerable)list)
                {
                    result.Add(new BsonDocument()
                        .Add("$id", new BsonValue(idField.Getter(item)))
                        .Add("$ref", collectionName));
                }

                return result;
            };

            p.Deserialize = (bson) =>
            {
                var array = bson.AsArray;
                var idField = itemMapper.Select(x => x.Value).FirstOrDefault(x => x.FieldName == "_id");

                // test if is an native array or a IList implementation
                if (listType.IsArray)
                {
                    var arr = Array.CreateInstance(itemType, array.Count);
                    var idx = 0;

                    foreach (var item in array)
                    {
                        var obj = Reflection.CreateInstance(itemType);
                        idField.Setter(obj, item.AsDocument["$id"].RawValue);
                        arr.SetValue(obj, idx++);
                    }

                    return arr;
                }
                else
                {
                    var list = (IList)Reflection.CreateInstance(listType);

                    foreach (var item in array)
                    {
                        var obj = Reflection.CreateInstance(itemType);
                        idField.Setter(obj, item.AsDocument["$id"].RawValue);
                        list.Add(obj);
                    }

                    return list;
                }
            };
        }

        #endregion

        /// <summary>
        /// Get a property based on a expression. Eg.: 'x => x.UserId' return string "UserId"
        /// </summary>
        private EntityBuilder<T> GetProperty<TK, K>(Expression<Func<TK, K>> expr, Action<PropertyMapper> action)
        {
            var member = expr.Body as MemberExpression;

            if (member == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a method, not a property.", expr.ToString()));
            }

            var prop = _prop[((PropertyInfo)member.Member).Name];

            action(prop);

            return this;
        }
    }
}
