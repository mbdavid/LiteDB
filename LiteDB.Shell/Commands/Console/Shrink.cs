using System;

namespace LiteDB.Shell.Commands
{
    internal class Shrink : ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"shrink\s*").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var password = s.Scan(".*").TrimToNull();

            display.WriteLine("Shrinking datafile..." + (password == "" ? "(no encryption)" : password));

            if (password != null)
            {
                display.WriteLine("Encrypting datafile using password: " + password);
            }

            display.WriteLine("Reduced: " + engine.Shrink(password) + " bytes");
        }
    }
}