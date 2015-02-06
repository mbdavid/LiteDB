using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class FileDelete : BaseGridFS, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "delete");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (db == null) throw new LiteException("No database");

            var id = this.ReadId(s);

            return db.GridFS.Delete(id);
        }
    }
}
