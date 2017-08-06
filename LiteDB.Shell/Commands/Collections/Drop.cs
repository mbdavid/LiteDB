using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionDrop : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop$");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);

            env.Display.WriteResult(env.Engine.DropCollection(col));
        }
    }
}