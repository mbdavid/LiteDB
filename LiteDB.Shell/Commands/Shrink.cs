using System;

namespace LiteDB.Shell.Commands
{
    internal class Shrink : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"db.shrink\s*").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            var password = s.Scan(".*").TrimToNull();

            env.Display.WriteLine("Shrinking datafile...");

            env.Display.WriteLine(password == null ?
                "No encryption" :
                "Encrypting datafile using password: " + password);

            env.Display.WriteLine("Reduced: " + env.Engine.Shrink(password) + " bytes");
        }
    }
}