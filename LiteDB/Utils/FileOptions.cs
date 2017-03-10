using System;

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
        public FileMode FileMode { get; set; }

        public FileOptions()
        {
            this.Journal = true;
            this.InitialSize = BasePage.PAGE_SIZE;
            this.LimitSize = long.MaxValue;
#if NET35
            this.FileMode = FileMode.Shared;
#endif
        }
    }

    public enum FileMode
    {
#if NET35
        Shared,
#endif
        Exclusive,
        ReadOnly
    }
}
