using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    interface ICommand
    {
        bool IsCommand(StringScanner s);
        void Execute(ref LiteEngine db, StringScanner s, Display display);
    }

    interface IShellCommand
    {
    }

    interface IWebCommand
    {
    }
}
