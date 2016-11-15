using System;

namespace LiteDB.Shell.Commands
{
    internal class FileDownload : BaseStorage, ICommand
    {
        public DataAccess Access { get { return DataAccess.Read; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "download");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);
            var filename = s.Scan(@"\s*.*").Trim();

            var file = fs.FindById(id);

            if (file != null)
            {
                file.SaveAs(filename);

                display.WriteResult(file.AsDocument);
            }
        }
    }
}