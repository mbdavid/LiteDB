using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class Open : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"open\s+").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            var connectionString = new ConnectionString(s.Scan(@".+").TrimToNull());

            env.Open(connectionString);
        }
    }
}