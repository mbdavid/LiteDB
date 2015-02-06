using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class FileUpdate : BaseFile, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "update");
        }

        public void Execute(LiteDatabase db, StringScanner s, Display display)
        {
            if (db == null) throw new LiteException("No database");

            var id = this.ReadId(s);

            var result = db.GridFS.SetMetadata(id, new JsonReader().ReadValue(s).AsObject);

            display.WriteBson(result);
        }
    }
}
