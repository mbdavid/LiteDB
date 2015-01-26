using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Quit : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"(quit|exit)$");
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            Environment.Exit(0);
        }
    }
}
