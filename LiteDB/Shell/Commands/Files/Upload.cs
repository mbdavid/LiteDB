using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class FilesUpload : Files, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "upload");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            var id = this.ReadId(s);

            var filename = Path.GetFullPath(s.Scan(@"\s*.*").Trim());

            if (!File.Exists(filename)) throw new IOException("File " + filename + " not found");

            var file = db.FileStorage.Upload(id, filename);

            display.WriteBson(file.AsDocument);
        }
    }
}
