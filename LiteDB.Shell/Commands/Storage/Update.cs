using System;

namespace LiteDB.Shell.Commands
{
    internal class FileUpdate : BaseStorage, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "update");
        }

        public BsonValue Execute(LiteEngine engine, StringScanner s)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);
            var metadata = JsonSerializer.Deserialize(s.ToString()).AsDocument;

            return fs.SetMetadata(id, metadata);
        }
    }
}