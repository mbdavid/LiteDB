using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class Collection<T>
    {
        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods. Useful for load reference documents when nedded.
        /// Returns a new Collection with this action included
        /// </summary>
        public Collection<T> Include(Action<T> action)
        {
            if (action == null) throw new ArgumentNullException("action");

            var col = new Collection<T>(_engine, Name);

            col._pageID = _pageID;
            col._includes.AddRange(_includes);

            col._includes.Add(action);

            return col;
        }
    }
}
