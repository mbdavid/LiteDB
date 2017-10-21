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
        /// Upgrade datafile from v6 to new v7 format used in LiteDB 3
        /// </summary>
        public static bool Upgrade(string filename, string password = null, bool backup = true, int batchSize = 5000)
        {
            // if not exists, just exit
            if (!File.Exists(filename)) return false;

            // use a temp file to copy/convert data from
            var tempFile = FileHelper.GetTempFile(filename);

            // open file as stream and test if is V6
            using(var stream = new FileStream(filename, System.IO.FileMode.Open, FileAccess.Read))
            {
                using (IDbReader reader = new LiteDB_V6.DbReader())
                {
                    if (reader.Initialize(stream, password) == false) return false;

                    // open new datafile to copy data from
                    using (var engine = new LiteEngine(tempFile, false))
                    {
                        foreach (var col in reader.GetCollections())
                        {
                            // first, create all unique indexes
                            foreach (var field in reader.GetUniqueIndexes(col))
                            {
                                engine.EnsureIndex(col, field, true);
                            }

                            // now bulk copy documents
                            var docs = reader.GetDocuments(col);

                            engine.InsertBulk(col, docs, batchSize);
                        }
                    }

                }
            }

            // if backup, move current file to new -bkp
            if (backup)
            {
                File.Move(filename, FileHelper.GetTempFile(filename, "-bkp"));
            }
            else
            {
                File.Delete(filename);
            }

            // move temp file to original filename
            File.Move(tempFile, filename);

            return true;
        }
    }
}
