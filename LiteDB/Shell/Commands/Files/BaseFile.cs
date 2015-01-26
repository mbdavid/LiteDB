using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class BaseFile
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
            return s.Scan(FileEntry.ID_PATTERN.Substring(1, FileEntry.ID_PATTERN.Length - 2));
        }
    }
}
