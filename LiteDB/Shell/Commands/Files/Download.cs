using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class FileDownload : BaseFile, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "download");
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            if (db == null) throw new LiteException("No database");

            var id = this.ReadId(s);
            var filename = s.Scan(@"\s*.*").Trim();

            var file = db.FileStorage.FindById(id);

            if (file != null)
            {
                file.SaveAs(filename, true);
                display.WriteBson(file.AsDocument);
            }
        }
    }
}
