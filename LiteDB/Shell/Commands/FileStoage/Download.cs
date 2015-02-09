using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class FileDownload : BaseFileStorage, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "download");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var id = this.ReadId(s);
            var filename = s.Scan(@"\s*.*").Trim();

            var file = db.FileStorage.FindById(id);

            if (file != null)
            {
                file.SaveAs(filename, true);

                return file.AsDocument;
            }
            else
            {
                return false;
            }
        }
    }
}
