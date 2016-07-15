using System;

namespace LiteDB.Shell.Commands
{
    internal class DbVersion : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"dbversion\s*").Length > 0;
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var ver = s.Scan(@"\d*");

            if (ver.Length > 0)
            {
                var v = Convert.ToUInt16(ver);
                engine.WriteDbVersion(v);
                return v;
            }
            else
            {
                return engine.ReadDbVersion();
            }
        }
    }
}