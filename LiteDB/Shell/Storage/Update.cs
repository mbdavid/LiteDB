using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    internal class FileUpdate : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "update");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);
            var metadata = JsonSerializer.Deserialize(s.ToString()).AsDocument;

            s.ThrowIfNotFinish();

            fs.SetMetadata(id, metadata);

            yield break;
        }
    }
}