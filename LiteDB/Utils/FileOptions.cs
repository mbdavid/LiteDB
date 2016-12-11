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
        public FileOpenMode FileMode { get; set; }

        public FileOptions()
        {
            this.Journal = true;
            this.InitialSize = BasePage.PAGE_SIZE;
            this.LimitSize = long.MaxValue;
            this.Timeout = TimeSpan.FromMinutes(1);
            this.FileMode = FileOpenMode.Shared;
        }
    }

    public enum FileOpenMode
    {
        Shared,
        ReadOnly,
        Exclusive
    }
}
