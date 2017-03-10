using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class ShowCollections : ICommand
    {
        public DataAccess Access { get { return DataAccess.Read; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"show\scollections$");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            if(engine == null)
            {
                display.WriteError("No database file currently open.");
                return;
            }

            var cols = engine.GetCollectionNames().OrderBy(x => x).ToArray();

            if (cols.Length > 0)
            {
                display.WriteLine(ConsoleColor.Cyan, string.Join(Environment.NewLine, cols));
            }
        }
    }
}