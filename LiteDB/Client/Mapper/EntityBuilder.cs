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
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

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
        /// Define a subdocument (or a list of) as a reference
        /// </summary>
        public EntityBuilder<T> DbRef<K>(Expression<Func<T, K>> property, string collection = null)
        {
            return this.GetProperty(property, (p) =>
            {
                BsonMapper.RegisterDbRef(_mapper, p, collection ?? _mapper.ResolveCollectionName(typeof(K)));
            });
        }

        /// <summary>
        /// Get a property based on a expression. Eg.: 'x => x.UserId' return string "UserId"
        /// </summary>
        private EntityBuilder<T> GetProperty<TK, K>(Expression<Func<TK, K>> property, Action<MemberMapper> action)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            var prop = _entity.GetMember(property);

            if (prop == null) throw new ArgumentNullException(property.GetPath());

            action(prop);

            return this;
        }
    }
}