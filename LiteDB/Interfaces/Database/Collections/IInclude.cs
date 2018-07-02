using System;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial interface ILiteCollection<T>
    {
        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        LiteCollection<T> Include<K>(Expression<Func<T, K>> path);

        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        LiteCollection<T> Include(string path);
    }
}
