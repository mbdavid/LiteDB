using System;

namespace LiteDB.Shell.Commands
{
    internal class UserVersion : ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"db.userversion\s*").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
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