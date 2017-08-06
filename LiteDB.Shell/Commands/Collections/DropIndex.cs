using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionDropIndex : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop[iI]ndex");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);
            var index = s.Scan(this.FieldPattern).Trim();

            env.Display.WriteResult(env.Engine.DropIndex(col, index));
        }
    }
}