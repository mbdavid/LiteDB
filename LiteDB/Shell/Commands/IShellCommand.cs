using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    public interface IShellCommand
    {
        bool IsCommand(StringScanner s);
        void Execute(LiteEngine db, StringScanner s, Display display);
    }
}
