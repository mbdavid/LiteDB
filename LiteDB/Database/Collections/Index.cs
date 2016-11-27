using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits
        /// </summary>
        /// <param name="field">Document field name (case sensitive)</param>
        /// <param name="unique">If is a unique index</param>
        public bool EnsureIndex(string field, bool unique = false)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");
            if (field == "_id") return false; // always exists

            if (!CollectionIndex.IndexPattern.IsMatch(field)) throw LiteException.InvalidFormat("IndexField", field);

            return _engine.Value.EnsureIndex(_name, field, unique);
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="property">Property linq expression</param>
        /// <param name="unique">Create a unique values index?</param>
        public bool EnsureIndex<K>(Expression<Func<T, K>> property, bool unique = false)
        {
            var field = _visitor.GetField(property);

            return this.EnsureIndex(field, unique);
        }

        /// <summary>
        /// Returns all indexes information
        /// </summary>
        public IEnumerable<IndexInfo> GetIndexes()
        {
            return _engine.Value.GetIndexes(_name);
        }

        /// <summary>
        /// Drop index and release slot for another index
        /// </summary>
        public bool DropIndex(string field)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            return _engine.Value.DropIndex(_name, field);
        }

        /// <summary>
        /// Create index based on a IndexNotFound exception
        /// </summary>
        private void EnsureIndex(IndexNotFoundException ex)
        {
            var unique = false;

            // try get if field are mapped as unique index (only if T isn't BsonDocument)
            if (typeof(T) != typeof(BsonDocument))
            {
                var entity = _mapper.GetEntityMapper(typeof(T));
                var member = entity.Members.FirstOrDefault(x => x.FieldName == ex.Field);
                unique = member == null ? false : member.IsUnique;
            }

            _engine.Value.EnsureIndex(ex.Collection, ex.Field, unique);
        }
    }
}