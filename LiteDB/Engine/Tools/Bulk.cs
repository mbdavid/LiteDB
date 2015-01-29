using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Bulk documents to a collection - use data chunks for most efficient insert
        /// </summary>
        public static int Bulk<T>(string connectionString, string collectionName, IEnumerable<T> docs, int buffer = 2000)
            where T : new()
        {
            if(string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");
            if(string.IsNullOrEmpty(collectionName)) throw new ArgumentNullException("collectionName");
            if(docs == null) throw new ArgumentNullException("collectionName");
            if(buffer < 100) throw new ArgumentException("buffer must be bigger than 100");

            var enumerator = docs.GetEnumerator();
            var count = 0;

            while (true)
            {
                var buff = buffer;

                using (var db = new LiteEngine(connectionString))
                {
                    var col = db.GetCollection<T>(collectionName);
                    var more = true;

                    db.BeginTrans();

                    while ((more = enumerator.MoveNext()) && buff > 0)
                    {
                        col.Insert(enumerator.Current);
                        buff--;
                        count++;
                    }

                    db.Commit();

                    if (more == false)
                    {
                        return count;
                    }
                }
            }
        }
    }
}
