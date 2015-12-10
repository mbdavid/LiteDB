using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class ShowCollections : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"show\scollections$");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var cols = engine.GetCollectionNames().OrderBy(x => x).ToArray();

            if (cols.Length == 0) return BsonValue.Null;

            return string.Join(Environment.NewLine, cols);
        }
    }
}