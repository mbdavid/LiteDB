using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class FileUpdate : BaseFile, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "update");
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            if (db == null) throw new LiteException("No database");

            var id = this.ReadId(s);
            var file = db.FileStorage.FindById(id);

            if (file == null) return;

            file.Metadata = new JsonReader().ReadValue(s).AsObject;

            db.FileStorage.Update(file);

            display.WriteBson(file.AsDocument);
        }
    }
}
