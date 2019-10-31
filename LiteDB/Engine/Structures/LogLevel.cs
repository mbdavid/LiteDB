using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Engine = 2,
        Command = 4,
        Query = 8,
        Disk = 16,
        Lock = 32,
        Sort = 64,
        Cache = 128,
        Transaction = 256
    }
}
