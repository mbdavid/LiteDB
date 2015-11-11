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
            if (buffer < 100) throw new ArgumentException("buffer must be bigger than 100");

            var enumerator = docs.GetEnumerator();
            var count = 0;

            while (true)
            {
                var buff = buffer;

                var more = true;

                while (buff > 0 && (more = enumerator.MoveNext()))
                {
                    this.Insert(enumerator.Current);
                    buff--;
                    count++;
                }

                if (more == false)
                {
                    return count;
                }
            }
        }
    }
}
