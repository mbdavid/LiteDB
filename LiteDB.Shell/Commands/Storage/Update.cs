using System;

namespace LiteDB.Shell.Commands
{
    internal class FileUpdate : BaseStorage, ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "update");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);
            var metadata = JsonSerializer.Deserialize(s.ToString()).AsDocument;

            fs.SetMetadata(id, metadata);
        }
    }
}