using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    [Help(
        Category = "FileStorage",
        Name = "find",
        Syntax = "fs.find [<fileId>]",
        Description = "List all files or filter them by fileId string key (use startsWith clause).",
        Examples = new string[] {
            "fs.find",
            "fs.find my_pho"
        }
    )]
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