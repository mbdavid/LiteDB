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
        public LiteCollection<T> Include<K>(Expression<Func<T, K>> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var value = _visitor.GetPath(path);

            return this.Include(value);
        }

        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        public LiteCollection<T> Include(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            // cloning this collection and adding this include
            var newcol = new LiteCollection<T>(_name, _engine, _mapper, _log);

            newcol._includes.AddRange(_includes);
            newcol._includes.Add(path);

            return newcol;
        }
    }
}