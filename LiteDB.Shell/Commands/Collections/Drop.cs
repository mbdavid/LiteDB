using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionDrop : BaseCollection, ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop$");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var col = this.ReadCollection(engine, s);

            display.WriteResult(engine.DropCollection(col));
        }
    }
}