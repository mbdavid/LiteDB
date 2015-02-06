using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Comment : ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"--");
        }

        public void Execute(LiteDatabase db, StringScanner s, Display display)
        {
        }
    }
}
