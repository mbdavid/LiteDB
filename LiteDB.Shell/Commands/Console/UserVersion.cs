using System;

namespace LiteDB.Shell.Commands
{
    internal class UserVersion : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"userversion\s*").Length > 0;
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
            var ver = s.Scan(@"\d*");

            if (ver.Length > 0)
            {
                engine.UserVersion = Convert.ToUInt16(ver);
            }
            else
            {
                display.WriteLine(engine.UserVersion.ToString());
            }
        }
    }
}