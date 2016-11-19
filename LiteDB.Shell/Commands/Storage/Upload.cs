using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class FileUpload : BaseStorage, ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "upload");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);

            var filename = s.Scan(@"\s*.*").Trim();

            if (!File.Exists(filename)) throw new IOException("File " + filename + " not found");

            var file = fs.Upload(id, filename);

            display.WriteResult(file.AsDocument);
        }
    }
}
