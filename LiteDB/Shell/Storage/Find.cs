using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    internal class FileFind : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "find");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var fs = new LiteStorage(engine);
            IEnumerable<LiteFileInfo> files;

            if (s.HasTerminated)
            {
                files = fs.FindAll();
            }
            else
            {
                var id = this.ReadId(s);

                s.ThrowIfNotFinish();

                files = fs.Find(id);
            }

            foreach (var file in files.Select(x => x.AsDocument))
            {
                yield return file;
            }
        }
    }
}