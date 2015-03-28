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

                return new BsonArray(files);
            }
            else
            {
                var id = this.ReadId(s);

                var files = db.FileStorage.Find(id).Select(x => x.AsDocument);

                return new BsonArray(files);
            }
        }
    }
}
