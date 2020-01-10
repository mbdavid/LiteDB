using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Globalization;
using static LiteDB.Constants;

namespace LiteDB
{
    /// <summary>
    /// Manage ConnectionString to connect and create databases. Connection string are NameValue using Name1=Value1; Name2=Value2
    /// </summary>
    public class ConnectionString
    {
        private readonly Dictionary<string, string> _values;

        /// <summary>
        /// "connection": Return how engine will be open (default: Direct)
        /// </summary>
        public ConnectionType Connection { get; set; } = ConnectionType.Direct;

        /// <summary>
        /// "filename": Full path or relative path from DLL directory
        /// </summary>
        public string Filename { get; set; } = "";

        /// <summary>
        /// "timeout": Timeout for waiting unlock operations (default: 1 minute)
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// "password": Database password used to encrypt/decypted data pages
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// "initial size": If database is new, initialize with allocated space - support KB, MB, GB (default: 0)
        /// </summary>
        public long? InitialSize { get; set; }

        /// <summary>
        /// "limit size": Max limit of datafile - support KB, MB, GB (default: long.MaxValue - no limit)
        /// </summary>
        public long? LimitSize { get; set; }

        /// <summary>
        /// "utc": Returns date in UTC timezone from BSON deserialization (default: false - LocalTime)
        /// </summary>
        public bool? UtcDate { get; set; } = false;

        /// <summary>
        /// "readonly": Open datafile in readonly mode (default: false)
        /// </summary>
        public bool? ReadOnly { get; set; } = false;

        /// <summary>
        /// "checkpoint": When LOG file get are bigger than checkpoint size (in pages), do a soft checkpoint (and also do a checkpoint at shutdown)
        /// Checkpoint = 0 means no auto-checkpoint and no shutdown checkpoint
        /// </summary>
        public int? Checkpoint { get; set; }

        /// <summary>
        /// "memory_segment_size":  Size, in PAGES, for each buffer array (used in MemoryStore) - Each byte array will be created with this size * PAGE_SIZE. 
        /// Should be > 100 (800kb) - Default: 1000 (8Mb each segment)
        /// </summary>
        public int? MemorySegmentSize { get; set; }

        /// <summary>
        /// "max_transaction_szie": Define, in page size, how many pages each collection will keep in memory before flush to disk. When reach this size
        /// all dirty pages will be saved on log files and clean pages will be removed from cache
        /// </summary>
        public int? MaxTransactionSize { get; set; }

        /// <summary>
        /// "culture": Define culture for this database. Value will persisted in disk at first write database. After this, there is no change of collation
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// "lcid": Define culture for this database. Can't be used with Culture. Persisted in datafile and can't be changed. Works only in first database creation
        /// </summary>
        public int? LCID { get; set; }

        /// <summary>
        /// "compare_options": Define how database will sort/compare strings. Can't be changed after database creation
        /// </summary>
        public CompareOptions? CompareOptions { get; set; }

        /// <summary>
        /// "upgrade": Check if data file is an old version and convert before open (default: false)
        /// </summary>
        public bool Upgrade { get; set; } = false;

        /// <summary>
        /// Initialize empty connection string
        /// </summary>
        public ConnectionString()
        {
            _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initialize connection string parsing string in "key1=value1;key2=value2;...." format or only "filename" as default (when no ; char found)
        /// </summary>
        public ConnectionString(string connectionString)
            : this()
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            // create a dictionary from string name=value collection
            if (connectionString.Contains("="))
            {
                _values.ParseKeyValue(connectionString);
            }
            else
            {
                _values["filename"] = connectionString;
            }

            // setting values to properties
            this.Connection = _values.GetValue("connection", this.Connection);
            this.Filename = _values.GetValue("filename", this.Filename).Trim();

            this.Password = _values.GetValue<string>("password");
            this.Timeout = _values.GetValue<TimeSpan?>("timeout");
            this.InitialSize = _values.GetFileSize(@"initial size");
            this.LimitSize = _values.GetFileSize(@"limit size");
            this.UtcDate = _values.GetValue("utc", this.UtcDate);
            this.ReadOnly = _values.GetValue<bool?>("readonly");

            this.Checkpoint = _values.GetValue<int?>("checkpoint");
            this.MaxTransactionSize = _values.GetValue<int?>("max_transaction_size");
            this.MemorySegmentSize = _values.GetValue<int?>("memory_segment_size");
            this.Culture = _values.GetValue<string>("culture");
            this.LCID = _values.GetValue<int?>("lcid");
            this.CompareOptions = _values.GetValue<CompareOptions?>("compare_options");

            this.Upgrade = _values.GetValue("upgrade", this.Upgrade);
        }

        /// <summary>
        /// Get value from parsed connection string. Returns null if not found
        /// </summary>
        public string this[string key] => _values.GetOrDefault(key);

        /// <summary>
        /// Create ILiteEngine instance according string connection parameters. For now, only Local/Shared are supported
        /// </summary>
        internal ILiteEngine CreateEngine()
        {
            var settings = new EngineSettings
            {
                Filename = this.Filename,
                Password = this.Password
            };/*
                InitialSize = this.InitialSize,
                LimitSize = this.LimitSize,
                UtcDate = this.UtcDate,
                Timeout = this.Timeout,
                ReadOnly = this.ReadOnly,
                Checkpoint = this.Checkpoint,
                MaxTransactionSize = this.MaxTransactionSize,
                MemorySegmentSize = this.MemorySegmentSize,
            };*/

            if (this.InitialSize != null) settings.InitialSize = this.InitialSize.Value;
            if (this.LimitSize != null) settings.LimitSize = this.LimitSize.Value;
            if (this.UtcDate != null) settings.UtcDate = this.UtcDate.Value;
            if (this.Timeout != null) settings.Timeout = this.Timeout.Value;
            if (this.ReadOnly != null) settings.ReadOnly = this.ReadOnly.Value;
            if (this.Checkpoint != null) settings.Checkpoint = this.Checkpoint.Value;
            if (this.MaxTransactionSize != null) settings.MaxTransactionSize = this.MaxTransactionSize.Value;
            if (this.MemorySegmentSize != null) settings.MemorySegmentSize = this.MemorySegmentSize.Value;

            if (this.LCID != null || this.Culture != null)
            {
                throw new NotImplementedException();
            }


            // create engine implementation as Connection Type
            if (this.Connection == ConnectionType.Direct)
            {
                return new LiteEngine(settings);
            }
            else if (this.Connection == ConnectionType.Shared)
            {
                return new SharedEngine(settings);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}