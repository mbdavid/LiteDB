using System;

namespace LiteDB.Shell.Commands
{
    internal class UserVersion : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"userversion\s*").Length > 0;
        }

        public BsonValue Execute(LiteEngine engine, StringScanner s)
        {
            var ver = s.Scan(@"\d*");

            if (ver.Length > 0)
            {
                var v = Convert.ToUInt16(ver);
                engine.UserVersion = v;
                return v;
            }
            else
            {
                return engine.UserVersion;
            }
        }
    }
}