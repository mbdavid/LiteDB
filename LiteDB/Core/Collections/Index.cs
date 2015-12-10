using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits
        /// </summary>
        /// <param name="field">Document field name (case sensitive)</param>
        /// <param name="options">All index options</param>
        public bool EnsureIndex(string field, IndexOptions options)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");
            if (options == null) throw new ArgumentNullException("options");
            if (field == "_id") return false; // always exists

            if (!CollectionIndex.IndexPattern.IsMatch(field)) throw LiteException.InvalidFormat("IndexField", field);

            return _engine.EnsureIndex(_name, field, options);
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits
        /// </summary>
        /// <param name="field">Document field name (case sensitive)</param>
        /// <param name="unique">All index options</param>
        public bool EnsureIndex(string field, bool unique = false)
        {
            return this.EnsureIndex(field, new IndexOptions { Unique = unique });
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="property">Property linq expression</param>
        /// <param name="unique">Create a unique values index?</param>
        public bool EnsureIndex<K>(Expression<Func<T, K>> property, bool unique = false)
        {
            var field = _visitor.GetBsonField(property);

            return this.EnsureIndex(field, new IndexOptions { Unique = unique });
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="property">Property linq expression</param>
        /// <param name="options">Use all indexes options</param>
        public bool EnsureIndex<K>(Expression<Func<T, K>> property, IndexOptions options)
        {
            var field = _visitor.GetBsonField(property);

            return this.EnsureIndex(field, options);
        }

        /// <summary>
        /// Returns all indexes in this collections
        /// </summary>
        public IEnumerable<BsonDocument> GetIndexes()
        {
            return _engine.GetIndexes(_name);
        }

        /// <summary>
        /// Drop index and release slot for another index
        /// </summary>
        public bool DropIndex(string field)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            return _engine.DropIndex(_name, field);
        }
    }
}