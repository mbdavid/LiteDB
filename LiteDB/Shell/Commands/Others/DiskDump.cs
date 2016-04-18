using System;

namespace LiteDB.Shell.Commands
{
    internal class DiskDump : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"diskdump\s*").Length > 0;
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            if (s.HasTerminated || s.Match(@"\d+"))
            {
                var start = s.Scan(@"\d*").Trim();
                var end = s.Scan(@"\s*\d*").Trim();

                if (start.Length > 0 && end.Length == 0) end = start;

                return engine.DumpPages(
                    start.Length == 0 ? 0 : Convert.ToUInt32(start),
                    end.Length == 0 ? uint.MaxValue : Convert.ToUInt32(end)).ToString();
            }
            else
            {
                var col = s.Scan(@"[\w-]+");
                var field = s.Scan(@"\s+\w+").Trim();

                return engine.DumpIndex(col, field).ToString();
            }
        }
    }
}