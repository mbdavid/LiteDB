using System;

namespace LiteDB.Shell.Commands
{
    internal class FileDownload : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "download");
        }

        public void Execute(StringScanner s, Env env)
        {
            var fs = new LiteStorage(env.Engine);
            var id = this.ReadId(s);
            var filename = s.Scan(@"\s*.*").Trim();

            var file = fs.FindById(id);

            if (file != null)
            {
                file.SaveAs(filename);

                env.Display.WriteResult(file.AsDocument);
            }
        }
    }
}