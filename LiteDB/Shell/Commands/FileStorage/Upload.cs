using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class FileUpload : BaseFileStorage, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "upload");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var fs = new LiteFileStorage(engine);
            var id = this.ReadId(s);
            var fileHandler = LitePlatform.Platform.FileHandler;

            var filename = s.Scan(@"\s*.*").Trim();

            if (!fileHandler.FileExists(filename)) throw new IOException("File " + filename + " not found");

            var file = fs.Upload(id, filename);

            return file.AsDocument;
        }
    }
}
