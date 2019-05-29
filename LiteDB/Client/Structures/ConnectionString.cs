using LiteDB.Engine;
using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Manage ConnectionString to connect and create databases. Connection string are NameValue using Name1=Value1; Name2=Value2
    /// </summary>
    public class ConnectionString
    {
        private readonly Dictionary<string, string> _values;

        /// <summary>
        /// "type": Return how engine will be open (default: Local)
        /// </summary>
        public ConnectionMode Mode { get; set; } = ConnectionMode.Exclusive;

        /// <summary>
        /// "filename": Full path or relative path from DLL directory
        /// Supported in [Local, Shared] connection type
        /// </summary>
        public string Filename { get; set; } = "";

        /// <summary>
        /// "timeout": Timeout for waiting unlock operations (default: 1 minute)
        /// Supported in [Local, Shared] connection type
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// "password": Database password used to encrypt/decypted data pages
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// "initial size": If database is new, initialize with allocated space - support KB, MB, GB (default: 0 bytes)
        /// Supported in [Local, Shared] connection type
        /// </summary>
        public long InitialSize { get; set; } = 0;

        /// <summary>
        /// "limit size": Max limit of datafile - support KB, MB, GB (default: long.MaxValue - no limit)
        /// Supported in [Local, Shared] connection type
        /// </summary>
        public long LimitSize { get; set; } = long.MaxValue;

        /// <summary>
        /// "utc": Returns date in UTC timezone from BSON deserialization (default: false - LocalTime)
        /// Supported in [Local, Shared] connection type
        /// </summary>
        public bool UtcDate { get; set; } = false;

        /// <summary>
        /// "readonly": Open datafile in readonly mode (default: false)
        /// Supported in [Local, Shared] connection type
        /// </summary>
        public bool ReadOnly { get; set; } = false;

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
            this.Mode = _values.GetValue("mode", this.Mode);
            this.Filename = _values.GetValue("filename", this.Filename);

            this.Password = _values.GetValue("password", this.Password);
            this.Timeout = _values.GetValue("timeout", this.Timeout);
            this.InitialSize = _values.GetFileSize(@"initial size", this.InitialSize);
            this.LimitSize = _values.GetFileSize(@"limit size", this.LimitSize);
            this.UtcDate = _values.GetValue("utc", this.UtcDate);
            this.ReadOnly = _values.GetValue("readonly", this.ReadOnly);
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
                Password = this.Password,
                InitialSize = this.InitialSize,
                LimitSize = this.LimitSize,
                UtcDate = this.UtcDate,
                Timeout = this.Timeout,
                ReadOnly = this.ReadOnly
            };

            // create engine implementation as Connection Type
            if (this.Mode == ConnectionMode.Exclusive)
            {
                return new LiteEngine(settings);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}