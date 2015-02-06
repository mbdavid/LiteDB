using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class FileUpdate : BaseGridFS, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "update");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (db == null) throw new LiteException("No database");

            var id = this.ReadId(s);
            var metadata = new JsonReader().ReadValue(s).AsObject;

            return db.GridFS.SetMetadata(id, metadata);
        }
    }
}
