using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    internal interface IShellCommand : ICommand
    {
        BsonValue Execute(LiteEngine engine, StringScanner s);
    }
}