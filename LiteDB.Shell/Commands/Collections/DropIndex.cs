using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionDropIndex : BaseCollection, ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop[iI]ndex");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var col = this.ReadCollection(engine, s);
            var index = s.Scan(this.FieldPattern).Trim();

            display.WriteResult(engine.DropIndex(col, index));
        }
    }
}