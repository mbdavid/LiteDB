using System;

namespace LiteDB.Shell
{
    internal class BaseStorage
    {
        public bool IsFileCommand(StringScanner s, string command)
        {
            return s.Scan(@"fs\." + command + @"\s*").Length > 0;
        }

        /// <summary>
        /// Read Id file
        /// </summary>
        public string ReadId(StringScanner s)
        {
            return s.Scan(LiteFileInfo.ID_PATTERN.Substring(1, LiteFileInfo.ID_PATTERN.Length - 2));
        }
    }
}