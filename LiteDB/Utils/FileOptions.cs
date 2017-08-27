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
#if NETSTANDARD
        public bool Async { get; set; }
#endif

        public FileOptions()
        {
            this.Journal = true;
            this.InitialSize = BasePage.PAGE_SIZE;
            this.LimitSize = long.MaxValue;
#if NETFULL
            this.FileMode = FileMode.Shared;
#else
            this.FileMode = FileMode.Exclusive;
#endif
        }
    }

    public enum FileMode
    {
#if NETFULL
        Shared,
#endif
        Exclusive,
        ReadOnly
    }
}
