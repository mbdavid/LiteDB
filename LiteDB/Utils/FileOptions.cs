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
#if HAVE_SYNC_OVER_ASYNC
        public bool Async { get; set; }
#endif
#if HAVE_FLUSH_DISK
        public bool Flush { get; set; } = false;
#endif

        public FileOptions()
        {
            this.Journal = true;
            this.InitialSize = 0;
            this.LimitSize = long.MaxValue;
#if HAVE_LOCK
            this.FileMode = FileMode.Shared;
#else
            this.FileMode = FileMode.Exclusive;
#endif
#if HAVE_FLUSH_DISK
            this.Flush = false;
#endif
        }
    }

    public enum FileMode
    {
#if HAVE_LOCK
        Shared,
#endif
        Exclusive,
        ReadOnly
    }
}
