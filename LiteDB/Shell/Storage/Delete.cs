using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    internal class FileDelete : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "delete");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);

            s.ThrowIfNotFinish();

            yield return fs.Delete(id);
        }
    }
}