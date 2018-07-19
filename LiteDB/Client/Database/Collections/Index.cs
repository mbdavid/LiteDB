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
        /// <param name="expression">Document field/expression</param>
        /// <param name="unique">If is a unique index</param>
        public bool EnsureIndex(string expression, bool unique = false)
        {
            if (string.IsNullOrEmpty(expression)) throw new ArgumentNullException(nameof(expression));

            return _engine.Value.EnsureIndex(_collection, expression, unique);
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits
        /// </summary>
        /// <param name="name">Index name</param>
        /// <param name="expression">Create a custom expression function to be indexed</param>
        /// <param name="unique">If is a unique index</param>
        public bool EnsureIndex(string name, string expression, bool unique = false)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(expression)) throw new ArgumentNullException(nameof(expression));

            return _engine.Value.EnsureIndex(_collection, name, expression, unique);
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="predicate">LinqExpression to be converted into BsonExpression to be indexed</param>
        /// <param name="unique">Create a unique keys index?</param>
        public bool EnsureIndex<K>(Expression<Func<T, K>> predicate, bool unique = false)
        {
            var expression = _mapper.GetExpression(predicate);

            return _engine.Value.EnsureIndex(_collection, expression.Source, unique);
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        public bool EnsureIndex<K>(string name, Expression<Func<T, K>> predicate, bool unique = false)
        {
            return _engine.Value.EnsureIndex(_collection, name, _mapper.GetExpression(predicate), unique);
        }

        /// <summary>
        /// Drop index and release slot for another index
        /// </summary>
        public bool DropIndex(string name)
        {
            return _engine.Value.DropIndex(_collection, name);
        }
    }
}