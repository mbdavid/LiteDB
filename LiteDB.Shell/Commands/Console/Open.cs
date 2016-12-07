using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class Open : ICommand
    {
        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"open\s+").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var connectionString = new ConnectionString(s.Scan(@".+").TrimToNull());

            env.Filename = connectionString.Filename;
            env.Password = connectionString.Password;
            env.Journal = connectionString.Journal;

            // create file if not exits
            if(!File.Exists(env.Filename))
            {
                using (var e = env.CreateEngine(DataAccess.Write))
                {
                }
            }
        }
    }
}