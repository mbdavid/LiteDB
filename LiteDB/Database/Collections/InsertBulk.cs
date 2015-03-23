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
            if (this.Database.Transaction.IsInTransaction) throw LiteException.InvalidTransaction();

            var enumerator = docs.GetEnumerator();
            var count = 0;

            while (true)
            {
                var buff = buffer;

                this.Database.Transaction.Begin();

                try
                {
                    var more = true;

                    while (buff > 0 && (more = enumerator.MoveNext()))
                    {
                        this.Insert(enumerator.Current);
                        buff--;
                        count++;
                    }

                    this.Database.Transaction.Commit();
                    this.Database.Cache.Clear(null);

                    if (more == false)
                    {
                        return count;
                    }
                }
                catch
                {
                    this.Database.Rollback();
                    throw;
                }
            }
        }
    }
}
