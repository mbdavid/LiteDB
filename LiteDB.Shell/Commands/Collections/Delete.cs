using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionDelete : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "delete");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);
            var query = this.ReadQuery(s);

            env.Display.WriteResult(env.Engine.Delete(col, query));
        }
    }
}