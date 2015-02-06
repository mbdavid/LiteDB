using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class FileFind : BaseGridFS, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "find");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (db == null) throw new LiteException("No database");

            if (s.HasTerminated)
            {
                var files = db.GridFS.FindAll().Select(x => x.AsDocument);

                return BsonArray.FromEnumerable<BsonDocument>(files);
            }
            else
            {
                var id = this.ReadId(s);

                if (id.EndsWith("*") || id.EndsWith("%"))
                {
                    var files = db.GridFS.Find(id.Substring(0, id.Length - 1)).Select(x => x.AsDocument);

                    return BsonArray.FromEnumerable<BsonDocument>(files);
                }
                else
                {
                    var file = db.GridFS.FindById(id);

                    return file != null ? file.AsDocument : BsonValue.Null;
                }
            }
        }
    }
}
