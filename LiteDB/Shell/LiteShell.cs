using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LiteDB.Shell.Commands;

namespace LiteDB.Shell
{
    internal class LiteShell
    {
        private static List<IShellCommand> _commands = new List<IShellCommand>();

        static LiteShell()
        {
            #region Register shell commands

            _commands.Add(new CollectionBulk());
            _commands.Add(new CollectionCount());
            _commands.Add(new CollectionDelete());
            _commands.Add(new CollectionDrop());
            _commands.Add(new CollectionDropIndex());
            _commands.Add(new CollectionEnsureIndex());
            _commands.Add(new CollectionFind());
            _commands.Add(new CollectionIndexes());
            _commands.Add(new CollectionInsert());
            _commands.Add(new CollectionMax());
            _commands.Add(new CollectionMin());
            _commands.Add(new CollectionRename());
            _commands.Add(new CollectionStats());
            _commands.Add(new CollectionUpdate());

            _commands.Add(new FileDelete());
            _commands.Add(new FileDownload());
            _commands.Add(new FileFind());
            _commands.Add(new FileUpdate());
            _commands.Add(new FileUpload());

            _commands.Add(new Comment());
            _commands.Add(new DbVersion());
            _commands.Add(new DiskDump());
            _commands.Add(new ShowCollections());
            _commands.Add(new Shrink());

            _commands.Add(new BeginTrans());
            _commands.Add(new CommitTrans());
            _commands.Add(new RollbackTrans());

#endregion
        }

        public BsonValue Run(DbEngine engine, string command)
        {
            if (string.IsNullOrEmpty(command)) return BsonValue.Null;

            var s = new StringScanner(command);

            foreach (var cmd in _commands)
            {
                if (cmd.IsCommand(s))
                {
                    return cmd.Execute(engine, s);
                }
            }

            throw LiteException.InvalidCommand(command);
        }
    }
}