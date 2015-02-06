using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class FileDownload : BaseGridFS, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "download");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (db == null) throw new LiteException("No database");

            var id = this.ReadId(s);
            var filename = s.Scan(@"\s*.*").Trim();

            var file = db.GridFS.FindById(id);

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
