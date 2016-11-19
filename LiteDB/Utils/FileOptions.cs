using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Datafile open options (for FileDiskService)
    /// </summary>
    public class FileOptions
    {
        public bool Journal { get; set; }
        public long InitialSize { get; set; }
        public long LimitSize { get; set; }
        public TimeSpan Timeout { get; set; }
        public bool ReadOnly { get; set; }

        public FileOptions()
        {
            this.Journal = true;
            this.InitialSize = BasePage.PAGE_SIZE;
            this.LimitSize = long.MaxValue;
            this.Timeout = TimeSpan.FromMinutes(1);
            this.ReadOnly = false;
        }
    }
}
