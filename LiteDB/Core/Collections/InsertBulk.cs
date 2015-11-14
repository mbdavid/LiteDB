using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Bulk documents to a collection - use data chunks for most efficient insert
        /// </summary>
        public int InsertBulk(IEnumerable<T> docs, int buffer = 32768)
        {
            if (docs == null) throw new ArgumentNullException("docs");
            if (buffer <= 1) throw new ArgumentException("buffer must be bigger than 1");

            var count = 0;

            while (true)
            {
                // get a slice of documents of docs
                var slice = docs.Skip(count).Take(buffer);

                // insert this docs
                var included = this.Insert(slice);

                // if included less than buffer, there is no more to insert
                if (included < buffer)
                {
                    return count + included;
                }

                count += included;
            }
        }
    }
}
