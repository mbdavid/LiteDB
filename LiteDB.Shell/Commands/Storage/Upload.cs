using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class FileUpload : BaseStorage, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "upload");
        }

        public BsonValue Execute(LiteEngine engine, StringScanner s)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);

            var filename = s.Scan(@"\s*.*").Trim();

            if (!File.Exists(filename)) throw new IOException("File " + filename + " not found");

            var file = fs.Upload(id, filename);

            return file.AsDocument;
        }
    }
}
