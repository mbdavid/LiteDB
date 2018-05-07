using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    [Help(
        Category = "Shell",
        Name = "show collections",
        Syntax = "show collections",
        Description = "List all collections inside datafile."
    )]
    internal class ShowCollections : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"show\scollections$");
        }

        public void Execute(StringScanner s, Env env)
        {
            var cols = env.Engine.GetCollectionNames().OrderBy(x => x).ToArray();

            if (cols.Length > 0)
            {
                env.Display.WriteLine(ConsoleColor.Cyan, string.Join(Environment.NewLine, cols));
            }
        }
    }
}