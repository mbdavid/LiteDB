using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionMax : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "max");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);
            var index = s.Scan(this.FieldPattern).Trim();

            env.Display.WriteResult(env.Engine.Max(col, index.Length == 0 ? "_id" : index));
        }
    }
}