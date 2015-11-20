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
        /// Define an index based in a property on entity
        /// </summary>
        public EntityBuilder<T> Index<K>(Expression<Func<T, K>> property, bool unique = false)
        {
            return this.GetProperty(property, (p) =>
            {
                p.IndexOptions = new IndexOptions { Unique = unique };
            });
        }

        /// <summary>
        /// Define an index based in a property on entity
        /// </summary>
        public EntityBuilder<T> Index<K>(Expression<Func<T, K>> property, IndexOptions options)
        {
            return this.GetProperty(property, (p) =>
            {
                p.IndexOptions = options;
            });
        }

        /// <summary>
        /// Define an index based in a field name on BsonDocument
        /// </summary>
        public EntityBuilder<T> Index(string field, bool unique = false)
        {
            return this.Index(field, new IndexOptions { Unique = unique });
        }

        /// <summary>
        /// Define an index based in a field name on BsonDocument
        /// </summary>
        public EntityBuilder<T> Index(string field, IndexOptions options)
        {
            var p = _prop.Values.FirstOrDefault(x => x.FieldName == field);

            if(p == null) throw new ArgumentException("field not found");

            p.IndexOptions = options;

            return this;
        }

        public EntityBuilder<T> Formula(string name, Func<T, BsonValue> getter)
        {
            // add a new property using a custom getter function
            var p = new PropertyMapper();
            p.FieldName = name;
            p.Getter = new GenericGetter((obj) => getter((T)obj));
            p.PropertyType = typeof(BsonValue);
            p.PropertyName = "__" + name;
            p.Setter = null; // readonly field

            _prop[p.PropertyName] = p;
            return this;
        }

        #region DbRef

        /// <summary>
        /// Define a subdocument (or a list of) as a reference
        /// </summary>
        public EntityBuilder<T> DbRef<K>(Expression<Func<T, K>> property, string collectionName)
        {
            return this.GetProperty(property, (p) =>
            {
                var typeRef = typeof(K);

                if (Reflection.IsList(typeRef))
                {
                    var itemType = typeRef.IsArray ? typeRef.GetElementType() : Reflection.UnderlyingTypeOf(typeRef);
                    var mapper = _mapper.GetPropertyMapper(itemType);

                    RegisterDbRefList(p, collectionName, typeRef, itemType, mapper);
                }
                else
                {
                    var mapper = _mapper.GetPropertyMapper(typeRef);

                    RegisterDbRef(p, collectionName, typeRef, mapper);
                }
            });
        }

        /// <summary>
        /// Register a property as a DbRef - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only 
        /// </summary>
        internal static void RegisterDbRef(PropertyMapper p, string collectionName, Type itemType, Dictionary<string, PropertyMapper> itemMapper)
        {
            p.Serialize = (obj, m) =>
            {
                var idField = itemMapper.Select(x => x.Value).FirstOrDefault(x => x.FieldName == "_id");

                var id = idField.Getter(obj);

                return new BsonDocument()
                    .Add("$id", new BsonValue(id))
                    .Add("$ref", collectionName);
            };

            p.Deserialize = (bson, m) =>
            {
                var idRef = bson.AsDocument["$id"];

                return m.Deserialize(itemType, 
                    idRef.IsNull ?
                    bson : // if has no $id object was full loaded (via Include) - so deserialize using normal function
                    new BsonDocument().Add("_id", idRef)); // if has $id, deserialize object using only _id object
            };
        }

        /// <summary>
        /// Register a property as a DbRefList - implement a custom Serialize/Deserialize actions to convert entity to $id, $ref only 
        /// </summary>
        internal static void RegisterDbRefList(PropertyMapper p, string collectionName, Type listType, Type itemType, Dictionary<string, PropertyMapper> itemMapper)
        {
            p.Serialize = (list, m) =>
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

            p.Deserialize = (bson, m) =>
            {
                var array = bson.AsArray;

                if(array.Count == 0) return m.Deserialize(listType, array);

                var hasIdRef = array[0].AsDocument["$id"].IsNull;

                if(hasIdRef)
                {
                    // if no $id, deserialize as full (was loaded via Include)
                    return m.Deserialize(listType, array);
                }
                else
                {
                    // copy array changing $id to _id
                    var arr = new BsonArray();

                    foreach(var item in array)
                    {
                        arr.Add(new BsonDocument().Add("_id", item.AsDocument["$id"]));
                    }

                    return m.Deserialize(listType, arr);
                }
            };
        }

        #endregion

        /// <summary>
        /// Get a property based on a expression. Eg.: 'x => x.UserId' return string "UserId"
        /// </summary>
        private EntityBuilder<T> GetProperty<TK, K>(Expression<Func<TK, K>> expr, Action<PropertyMapper> action)
        {
            var prop = _prop[expr.GetPath()];

            if(prop == null) throw new ArgumentNullException(expr.GetPath());

            action(prop);

            return this;
        }
    }
}
