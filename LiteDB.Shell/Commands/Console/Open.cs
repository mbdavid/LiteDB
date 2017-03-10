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
            env.ConnectionString = new ConnectionString(s.Scan(@".+").TrimToNull());

            // if needs upgrade, do it now
            if (env.ConnectionString.Upgrade)
            {
                display.WriteLine("Upgrading datafile...");

                var result = LiteEngine.Upgrade(env.ConnectionString.Filename, env.ConnectionString.Password);

                if (result)
                {
                    display.WriteLine("Datafile upgraded to V7 format (LiteDB v.3.x)");
                }
                else
                {
                    throw new ShellException("File format do not support upgrade (" + env.ConnectionString.Filename + ")");
                }

                env.ConnectionString.Upgrade = false;
            }
            else
            {
                var isNew = File.Exists(env.ConnectionString.Filename);

                // open datafile just to test if it's ok (or to create new)
                using (var e = env.CreateEngine(isNew ? DataAccess.Write : DataAccess.Read))
                {
                }
            }
        }
    }
}