using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        public LiteCollection<T> Include<K>(Expression<Func<T, K>> dbref)
        {
            if (dbref == null) throw new ArgumentNullException("dbref");

            var col = new LiteCollection<T>(this.Database, Name);

            col._pageID = _pageID;
            col._includes.AddRange(_includes);

            var mapper = this.Database.Mapper.GetPropertyMapper(typeof(T));
            var prop = mapper[""];

            Action<T> action = (o) =>
            {

            };


            //col._includes.Add(action);

            return col;
        }
    }
}
