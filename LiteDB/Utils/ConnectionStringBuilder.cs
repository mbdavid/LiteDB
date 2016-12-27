using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    public class ConnectionStringBuilder
    {
        internal const int DefaultCacheSize = 5000;
        internal static readonly TimeSpan DefaultTimeOut = TimeSpan.FromMinutes(1);
#if NET35
        internal const FileMode DefaultFileMode = FileMode.Shared;
#else
        internal const FileMode DefaultFileMode = FileMode.Exclusive;
#endif
        internal const long DefaultInitialSize = BasePage.PAGE_SIZE * 2;
        internal const long DefaultLimitSize = long.MaxValue;
        internal const byte DefaultLogLevel = Logger.NONE;
        internal const bool DefaultJournal = true;
        internal const bool DefaultUpgrade = false;

        public ConnectionStringBuilder()
        {
            Filename = string.Empty;
            Journal = DefaultJournal;
            Password = null;
            CacheSize = DefaultCacheSize;
            Timeout = DefaultTimeOut;
            Mode = DefaultFileMode;
            InitialSize = DefaultInitialSize;
            LimitSize = DefaultLimitSize;
            Log = DefaultLogLevel;
            Upgrade = DefaultUpgrade;
        }

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
        /// "mode": Define if datafile will be shared, exclusive or read only access (default: Shared)
        /// </summary>
        public FileMode Mode { get; set; }

        /// <summary>
        /// "initial size": If database is new, initialize with allocated space - support KB, MB, GB (default: null)
        /// </summary>
        public long InitialSize { get; set; }

        /// <summary>
        /// "limit size": Max limit of datafile - support KB, MB, GB (default: null)
        /// </summary>
        public long LimitSize { get; set; }

        /// <summary>
        /// "log": Debug messages from database - use `LiteDatabase.Log` (default: Logger.NONE)
        /// </summary>
        public byte Log { get; set; }

        /// <summary>
        /// "upgrade": Test if database is in old version and update if needed (default: false)
        /// </summary>
        public bool Upgrade { get; set; }

        public ConnectionString ToConnectionString()
        {
            return new ConnectionString(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Filename))
            {
                sb.AppendFormat("filename=\"{0}\";", Filename);
            }
            if (Journal != DefaultJournal)
            {
                sb.AppendFormat("journal={0};", DefaultJournal);
            }
            if (!string.IsNullOrEmpty(Password))
            {
                sb.AppendFormat("password=\"{0}\";", Password);
            }
            if (CacheSize != DefaultCacheSize)
            {
                sb.AppendFormat("cache size={0};", CacheSize);
            }
            if (Timeout != DefaultTimeOut)
            {
                sb.AppendFormat("timeout={0};", Timeout);
            }
            if (Mode != DefaultFileMode)
            {
                sb.AppendFormat("mode={0};", Mode);
            }
            if (InitialSize != DefaultInitialSize)
            {
                sb.AppendFormat("initial size={0};", InitialSize);
            }
            if (LimitSize != DefaultLimitSize)
            {
                sb.AppendFormat("limit size={0};", LimitSize);
            }
            if (Log != DefaultLogLevel)
            {
                sb.AppendFormat("log={0};", Log);
            }
            if (Upgrade != DefaultUpgrade)
            {
                sb.AppendFormat("upgrade={0};", Upgrade);
            }
            return sb.ToString();
        }
    }
}
