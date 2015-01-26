using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Close : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"close$").Length > 0;
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            db.Dispose();
        }
    }
}
