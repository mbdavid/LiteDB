using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionUpdate : BaseCollection, ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "update");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var col = this.ReadCollection(engine, s);
            var doc = JsonSerializer.Deserialize(s.ToString()).AsDocument;

            display.WriteResult(engine.Update(col, doc));
        }
    }
}