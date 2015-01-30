using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class FileFind : BaseFile, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "find");
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            if (db == null) throw new LiteException("No database");

            if (s.HasTerminated)
            {
                display.WriteBson<BsonDocument>(db.FileStorage.All().Select(x => x.AsDocument));
            }
            else
            {
                var id = this.ReadId(s);

                if (id.EndsWith("*") || id.EndsWith("%"))
                {
                    display.WriteBson<BsonDocument>(db.FileStorage.Find(id.Substring(0, id.Length - 1)).Select(x => x.AsDocument));
                }
                else
                {
                    var file = db.FileStorage.FindById(id);

                    if (file != null)
                    {
                        display.WriteBson(file.AsDocument);
                    }
                }
            }
        }
    }
}
