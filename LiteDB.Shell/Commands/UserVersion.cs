using System;

namespace LiteDB.Shell.Commands
{
    internal class UserVersion : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"db.userversion\s*").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            var ver = s.Scan(@"\d*");

            if (ver.Length > 0)
            {
                env.Engine.UserVersion = Convert.ToUInt16(ver);
            }
            else
            {
                env.Display.WriteLine(env.Engine.UserVersion.ToString());
            }
        }
    }
}