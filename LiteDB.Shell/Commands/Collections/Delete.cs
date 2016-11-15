using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionDelete : BaseCollection, ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "delete");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var col = this.ReadCollection(engine, s);
            var query = this.ReadQuery(s);

            display.WriteResult(engine.Delete(col, query));
        }
    }
}