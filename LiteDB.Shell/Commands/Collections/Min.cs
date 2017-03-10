using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionMin : BaseCollection, ICommand
    {
        public DataAccess Access { get { return DataAccess.Read; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "min");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var col = this.ReadCollection(engine, s);
            var index = s.Scan(this.FieldPattern).Trim();

            display.WriteResult(engine.Min(col, index.Length == 0 ? "_id" : index));
        }
    }
}