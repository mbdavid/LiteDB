using System.Collections.Generic;
using System.IO;

namespace LiteDB.Shell
{
    [Help(
        Category = "FileStorage",
        Name = "upload",
        Syntax = "fs.upload <fileId> <filename>",
        Description = "Upload a local file to data file.",
        Examples = new string[] {
            "fs.upload my_photo_001 c:/Temp/my_photo_001.jpg"
        }
    )]
    internal class FileUpload : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "upload");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);

            var filename = s.Scan(@"\s*.*").Trim();

            if (!File.Exists(filename)) throw new IOException("File " + filename + " not found");

            var file = fs.Upload(id, filename);

            yield return file.AsDocument;
        }
    }
}
