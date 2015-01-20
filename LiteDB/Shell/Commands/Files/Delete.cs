using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class FilesDelete : Files, ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "delete");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            var id = this.ReadId(s);

            display.WriteBson(db.FileStorage.Delete(id));
        }
    }
}
