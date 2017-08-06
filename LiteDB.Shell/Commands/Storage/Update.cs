using System;

namespace LiteDB.Shell.Commands
{
    internal class FileUpdate : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "update");
        }

        public void Execute(StringScanner s, Env env)
        {
            var fs = new LiteStorage(env.Engine);
            var id = this.ReadId(s);
            var metadata = JsonSerializer.Deserialize(s.ToString()).AsDocument;

            fs.SetMetadata(id, metadata);
        }
    }
}