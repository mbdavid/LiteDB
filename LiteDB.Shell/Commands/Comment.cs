using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Comment : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"--");
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
        }
    }
}
