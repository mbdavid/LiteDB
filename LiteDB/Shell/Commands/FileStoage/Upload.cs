using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class FileUpload : BaseFileStorage, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "upload");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var id = this.ReadId(s);

            var filename = Path.GetFullPath(s.Scan(@"\s*.*").Trim());

            if (!File.Exists(filename)) throw new IOException("File " + filename + " not found");

            var file = db.FileStorage.Upload(id, filename);

            return file.AsDocument;
        }
    }
}
