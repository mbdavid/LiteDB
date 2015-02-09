using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class FileDelete : BaseFileStorage, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "delete");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var id = this.ReadId(s);

            return db.FileStorage.Delete(id);
        }
    }
}
