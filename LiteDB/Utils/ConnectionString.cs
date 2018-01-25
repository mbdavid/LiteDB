using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Manage ConnectionString to connect and create databases. Connection string are NameValue using Name1=Value1; Name2=Value2
    /// </summary>
    public class ConnectionString
    {
        /// <summary>
        /// "filename": Full path or relative path from DLL directory
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// "journal": Enabled or disable double write check to ensure durability (default: true)
        /// </summary>
        public bool Journal { get; set; }

        /// <summary>
        /// "password": Encrypt (using AES) your datafile with a password (default: null - no encryption)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// "cache size": Max number of pages in cache. After this size, flush data to disk to avoid too memory usage (default: 5000)
        /// </summary>
        public int CacheSize { get; set; }

        /// <summary>
        /// "timeout": Timeout for waiting unlock operations (default: 1 minute)
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// "mode": Define if datafile will be shared, exclusive or read only access (default in environments with file locking: Shared, otherwise: Exclusive)
        /// </summary>
        public FileMode Mode { get; set; }

        /// <summary>
        /// "initial size": If database is new, initialize with allocated space - support KB, MB, GB (default: 0 bytes)
        /// </summary>
        public long InitialSize { get; set; }

        /// <summary>
        /// "limit size": Max limit of datafile - support KB, MB, GB (default: long.MaxValue - no limit)
        /// </summary>
        public long LimitSize { get; set; }

        /// <summary>
        /// "log": Debug messages from database - use `LiteDatabase.Log` (default: Logger.NONE)
        /// </summary>
        public byte Log { get; set; }

        /// <summary>
        /// "utc": Returns date in UTC timezone from BSON deserialization (default: false - LocalTime)
        /// </summary>
        public bool UtcDate { get; set; }

        /// <summary>
        /// "upgrade": Test if database is in old version and update if needed (default: false)
        /// </summary>
        public bool Upgrade { get; set; }

#if HAVE_SYNC_OVER_ASYNC
        /// <summary>
        /// "async": Use "sync over async" to UWP apps access any directory (default: false)
        /// </summary>
        public bool Async { get; set; }
#endif

        /// <summary>
        /// Initialize empty connection string
        /// </summary>
        public ConnectionString()
        {
            this.Filename = "";
            this.Journal = true;
            this.Password = null;
            this.CacheSize = 5000;
            this.Timeout = TimeSpan.FromMinutes(1);
#if HAVE_LOCK
            this.Mode = FileMode.Shared;
#else
            this.Mode = FileMode.Exclusive;
#endif
            this.InitialSize = 0;
            this.LimitSize = long.MaxValue;
            this.Log = Logger.NONE;
            this.UtcDate = false;
            this.Upgrade = false;
#if HAVE_SYNC_OVER_ASYNC
            this.Async = false;
#endif
        }

        /// <summary>
        /// Initialize connection string parsing string in "key1=value1;key2=value2;...." format or only "filename" as default (when no ; char found)
        /// </summary>
        public ConnectionString(string connectionString)
            : this()
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // create a dictionary from string name=value collection
            if (connectionString.Contains("="))
            {
                values.ParseKeyValue(connectionString);
            }
            else
            {
                values["filename"] = connectionString;
            }

            // setting values to properties
            this.Filename = values.GetValue("filename", this.Filename);
            this.Journal = values.GetValue("journal", this.Journal);
            this.Password = values.GetValue<string>("password", this.Password);
            this.CacheSize = values.GetValue(@"cache size", this.CacheSize);
            this.Timeout = values.GetValue("timeout", this.Timeout);
            this.Mode = values.GetValue("mode", this.Mode);
            this.InitialSize = values.GetFileSize(@"initial size", this.InitialSize);
            this.LimitSize = values.GetFileSize(@"limit size", this.LimitSize);
            this.Log = values.GetValue("log", this.Log);
            this.UtcDate = values.GetValue("utc", this.UtcDate);
            this.Upgrade = values.GetValue("upgrade", this.Upgrade);
#if HAVE_SYNC_OVER_ASYNC
            this.Async = values.GetValue("async", this.Async);
#endif
        }
    }
}