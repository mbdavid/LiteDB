using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class ShowCollections : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"show\scollections$");
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
            var cols = engine.GetCollectionNames().OrderBy(x => x).ToArray();

            if (cols.Length > 0)
            {
                display.WriteLine(ConsoleColor.Cyan, string.Join(Environment.NewLine, cols));
            }
        }
    }
}