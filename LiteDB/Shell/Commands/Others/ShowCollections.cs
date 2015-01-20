using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class ShowCollections : ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"show\scollections");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            display.WriteResult(string.Join("\n", db.GetCollections().OrderBy(x => x).ToArray()));
        }
    }
}
