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
            return this.EnsureIndex(field, null, unique);
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits
        /// </summary>
        /// <param name="field">Document field name (case sensitive)</param>
        /// <param name="expression">Create a custom expression function to be indexed</param>
        /// <param name="unique">If is a unique index</param>
        public bool EnsureIndex(string field, string expression, bool unique = false)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException(nameof(field));

            return _engine.Value.EnsureIndex(_name, field, expression, unique);
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="property">Property linq expression</param>
        /// <param name="unique">Create a unique keys index?</param>
        public bool EnsureIndex<K>(Expression<Func<T, K>> property, bool unique = false)
        {
            return this.EnsureIndex(property, null, unique);
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="property">Property linq expression</param>
        /// <param name="expression">Create a custom expression function to be indexed</param>
        /// <param name="unique">Create a unique keys index?</param>
        public bool EnsureIndex<K>(Expression<Func<T, K>> property, string expression, bool unique = false)
        {
            var field = _visitor.GetField(property);

            return this.EnsureIndex(field, expression ?? _visitor.GetPath(property), unique);
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
            return _engine.Value.DropIndex(_name, field);
        }
    }
}