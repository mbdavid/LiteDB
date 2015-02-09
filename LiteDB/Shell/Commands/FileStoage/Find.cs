using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class FileFind : BaseFileStorage, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "find");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (s.HasTerminated)
            {
                var files = db.FileStorage.FindAll().Select(x => x.AsDocument);

                return BsonArray.FromEnumerable<BsonDocument>(files);
            }
            else
            {
                var id = this.ReadId(s);

                if (id.EndsWith("*") || id.EndsWith("%"))
                {
                    var files = db.FileStorage.Find(id.Substring(0, id.Length - 1)).Select(x => x.AsDocument);

                    return BsonArray.FromEnumerable<BsonDocument>(files);
                }
                else
                {
                    var file = db.FileStorage.FindById(id);

                    return file != null ? file.AsDocument : BsonValue.Null;
                }
            }
        }
    }
}
