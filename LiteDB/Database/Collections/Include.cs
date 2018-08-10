using System;
using System.Collections.Generic;
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

            return Include(new string[] { path });
        }

        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        /// <param name="paths">Property paths to include.</param>
        public LiteCollection<T> Include(string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            // cloning this collection and adding this include
            var newcol = new LiteCollection<T>(_name, _engine, _mapper, _log);

            newcol._includes.AddRange(_includes);

            // add all paths that are not null nor empty due to previous check
            newcol._includes.AddRange(paths.Where(x => !String.IsNullOrEmpty(x)));

            return newcol;
        }

        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load all DbRef documents
        /// Returns a new Collection with this actions included
        /// </summary>
        /// <param name="maxDepth">Maximum recersive depth of the properties to include, use -1 (default) to include all.</param>
        public LiteCollection<T> IncludeAll(int maxDepth = -1)
        {
            return Include(GetRecursivePaths(typeof(T), maxDepth, 0));
        }

        /// <summary>
        /// Recursively get all db ref paths.
        /// </summary>
        /// <returns>All the paths found during recursion.</returns>
        private string[] GetRecursivePaths(Type pathType, int maxDepth, int currentDepth, string basePath = null)
        {
            currentDepth++;

            var paths = new List<string>();

            if (maxDepth < 0 || currentDepth <= maxDepth)
            {
                var fields = _mapper.GetEntityMapper(pathType).Members.Where(x => x.IsDbRef);

                basePath = string.IsNullOrEmpty(basePath) ? "$" : basePath;

                foreach (var field in fields)
                {
                    var path = field.IsList ? $"{basePath}.{field.FieldName}[*]" : $"{basePath}.{field.FieldName}";
                    paths.Add(path);
                    paths.AddRange(GetRecursivePaths(field.UnderlyingType, maxDepth, currentDepth, path));
                }
            }

            return paths.ToArray();
        }
    }
}