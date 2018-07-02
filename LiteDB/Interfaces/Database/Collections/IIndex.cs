using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial interface ILiteCollection<T>
    {
        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits
        /// </summary>
        /// <param name="field">Document field name (case sensitive)</param>
        /// <param name="unique">If is a unique index</param>
        bool EnsureIndex(string field, bool unique = false);

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits
        /// </summary>
        /// <param name="field">Document field name (case sensitive)</param>
        /// <param name="expression">Create a custom expression function to be indexed</param>
        /// <param name="unique">If is a unique index</param>
        bool EnsureIndex(string field, string expression, bool unique = false);

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="property">Property linq expression</param>
        /// <param name="unique">Create a unique keys index?</param>
        bool EnsureIndex<K>(Expression<Func<T, K>> property, bool unique = false);

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="property">Property linq expression</param>
        /// <param name="expression">Create a custom expression function to be indexed</param>
        /// <param name="unique">Create a unique keys index?</param>
        bool EnsureIndex<K>(Expression<Func<T, K>> property, string expression, bool unique = false);

        /// <summary>
        /// Returns all indexes information
        /// </summary>
        IEnumerable<IndexInfo> GetIndexes();

        /// <summary>
        /// Drop index and release slot for another index
        /// </summary>
        bool DropIndex(string field);
    }
}