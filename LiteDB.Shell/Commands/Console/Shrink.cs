using System;

namespace LiteDB.Shell.Commands
{
    internal class Shrink : ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"db.shrink\s*").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var password = s.Scan(".*").TrimToNull();

            display.WriteLine("Shrinking datafile...");

            display.WriteLine(password == null ?
                "No encryption" :
                "Encrypting datafile using password: " + password);

            display.WriteLine("Reduced: " + engine.Shrink(password) + " bytes");
        }
    }
}