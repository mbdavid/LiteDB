using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Quit : ICommand, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"(quit|exit)$");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            Environment.Exit(0);
        }
    }
}
