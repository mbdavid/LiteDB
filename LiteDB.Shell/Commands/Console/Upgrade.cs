using System;
using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class Upgrade : ICommand
    {
        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"upgrade\s+").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var connectionString = new ConnectionString(s.Scan(@".+").TrimToNull());

            display.WriteLine("Upgrading datafile...");

            var result = LiteEngine.Upgrade(connectionString.Filename, connectionString.Password);

            if(result)
            {
                display.WriteLine("Datafile upgraded to V7 format (LiteDB v.3.x)");
            }
            else
            {
                throw new ShellException("File format do not support upgrade (" + connectionString.Filename + ")");
            }
        }
    }
}