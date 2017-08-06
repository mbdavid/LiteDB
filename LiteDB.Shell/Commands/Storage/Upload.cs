using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class FileUpload : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "upload");
        }

        public void Execute(StringScanner s, Env env)
        {
            var fs = new LiteStorage(env.Engine);
            var id = this.ReadId(s);

            var filename = s.Scan(@"\s*.*").Trim();

            if (!File.Exists(filename)) throw new IOException("File " + filename + " not found");

            var file = fs.Upload(id, filename);

            env.Display.WriteResult(file.AsDocument);
        }
    }
}
