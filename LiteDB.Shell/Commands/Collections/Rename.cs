using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionRename : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "rename");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);
            var newName = s.Scan(@"[\w-]+").ThrowIfEmpty("Invalid new collection name");

            env.Display.WriteResult(env.Engine.RenameCollection(col, newName));
        }
    }
}