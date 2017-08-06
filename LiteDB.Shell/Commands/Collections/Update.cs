using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionUpdate : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "update");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);
            var doc = JsonSerializer.Deserialize(s.ToString()).AsDocument;

            env.Display.WriteResult(env.Engine.Update(col, doc));
        }
    }
}