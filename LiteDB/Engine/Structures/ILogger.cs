using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    public interface ILogger
    {
        LogLevel Level { get; }

        void Log(LogLevel level, string message);
    }
}
