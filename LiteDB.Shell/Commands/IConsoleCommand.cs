using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    internal interface IConsoleCommand : ICommand
    {
        void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input);
    }
}