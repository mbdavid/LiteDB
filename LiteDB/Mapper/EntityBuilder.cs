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
                _entity.Members.Remove(p);
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
                p.IsUnique = unique;
            });
        }

        /// <summary>
        /// Define an index based in virtual property (getter function)
        /// </summary>
        public EntityBuilder<T> Index<K>(string indexName, Func<T, BsonValue> getter, bool unique = false)
        {
            _entity.Members.Add(new MemberMapper
            {
                FieldName = indexName,
                MemberName = indexName,
                Getter = x => (object)getter((T)x),
                Setter = null,
                DataType = typeof(BsonValue),
                IsUnique = unique
            });

            return this;
        }

        /// <summary>
        /// Define an index based in a field name on BsonDocument
        /// </summary>
        public EntityBuilder<T> Index(string field, bool unique = false)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            var p = _entity.Members.FirstOrDefault(x => x.FieldName == field);

            if (p == null) throw new ArgumentException("field not found");

            p.IsUnique = unique;

            return this;
        }

        /// <summary>
        /// Define a subdocument (or a list of) as a reference
        /// </summary>
        public EntityBuilder<T> DbRef<K>(Expression<Func<T, K>> property, string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName)) throw new ArgumentNullException("collectionName");

            return this.GetProperty(property, (p) =>
            {
                BsonMapper.RegisterDbRef(_mapper, p, collectionName);
            });
        }

        /// <summary>
        /// Get a property based on a expression. Eg.: 'x => x.UserId' return string "UserId"
        /// </summary>
        private EntityBuilder<T> GetProperty<TK, K>(Expression<Func<TK, K>> expr, Action<MemberMapper> action)
        {
            if (expr == null) throw new ArgumentNullException("property");

            var prop = _entity.GetMember(expr);

            if (prop == null) throw new ArgumentNullException(expr.GetPath());

            action(prop);

            return this;
        }
    }
}