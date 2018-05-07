using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    [Help(
        Category = "FileStorage",
        Name = "find",
        Syntax = "fs.download <fileId> <filename>",
        Description = "Download file inside storage to local computer. Returns true if file has been saved on local disk.",
        Examples = new string[] {
            "fs.download my_photo001 C:/Temp/my_photo001.jpg"
        }
    )]
    internal class FileDownload : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "download");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);
            var filename = s.Scan(@"\s*.*").Trim();

            var file = fs.FindById(id);

            if (file != null)
            {
                file.SaveAs(filename);

                yield return file.AsDocument;
            }
        }
    }
}