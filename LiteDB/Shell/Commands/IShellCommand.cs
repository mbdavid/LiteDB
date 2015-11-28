using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    internal interface IShellCommand
    {
        bool IsCommand(StringScanner s);
        BsonValue Execute(DbEngine engine, StringScanner s);
    }
}
