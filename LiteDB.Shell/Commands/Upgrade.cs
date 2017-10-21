using System;
using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class Upgrade : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"upgrade\s+").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            var connectionString = new ConnectionString(s.Scan(@".+").TrimToNull());

            env.Display.WriteLine("Upgrading datafile...");

            var result = LiteEngine.Upgrade(connectionString.Filename, connectionString.Password);

            if(result)
            {
                env.Display.WriteLine("Datafile upgraded to V7 format (LiteDB v.3.x)");
            }
            else
            {
                throw new ShellException("File format do not support upgrade (" + connectionString.Filename + ")");
            }
        }
    }
}