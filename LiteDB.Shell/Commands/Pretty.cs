using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Pretty : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"pretty\s*").Length > 0;
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            display.Pretty = !(s.Scan(@"off\s*").Length > 0);
        }
    }
}
