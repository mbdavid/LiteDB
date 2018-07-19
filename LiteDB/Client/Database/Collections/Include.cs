using System;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        public LiteCollection<T> Include<K>(Expression<Func<T, K>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            var path = _mapper.GetExpression(predicate);

            return this.Include(path);
        }

        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        public LiteCollection<T> Include(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            // cloning this collection and adding this include
            var newcol = new LiteCollection<T>(_collection, _engine, _mapper);

            newcol._includes.AddRange(_includes);
            newcol._includes.Add(path);

            return newcol;
        }
    }
}