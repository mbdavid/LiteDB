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
        /// <summary>
        /// "filename": Full path or relative path from DLL directory
        /// </summary>
        public string Filename { get; set; } = "";

        /// <summary>
        /// "password": Encrypt (using AES) your datafile with a password (default: null - no encryption)
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// "timeout": Timeout for waiting unlock operations (default: 1 minute)
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// "initial size": If database is new, initialize with allocated space - support KB, MB, GB (default: 0 bytes)
        /// </summary>
        public long InitialSize { get; set; } = 0;

        /// <summary>
        /// "limit size": Max limit of datafile - support KB, MB, GB (default: long.MaxValue - no limit)
        /// </summary>
        public long LimitSize { get; set; } = long.MaxValue;

        /// <summary>
        /// "log": Debug messages from database - use `LiteDatabase.Log` (default: Logger.NONE)
        /// </summary>
        public byte Log { get; set; } = Logger.NONE;

        /// <summary>
        /// "utc": Returns date in UTC timezone from BSON deserialization (default: false - LocalTime)
        /// </summary>
        public bool UtcDate { get; set; } = false;

        /// <summary>
        /// Initialize empty connection string
        /// </summary>
        public ConnectionString()
        {
        }

        /// <summary>
        /// Initialize connection string parsing string in "key1=value1;key2=value2;...." format or only "filename" as default (when no ; char found)
        /// </summary>
        public ConnectionString(string connectionString)
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
            this.Password = values.GetValue<string>("password", this.Password);
            this.Timeout = values.GetValue("timeout", this.Timeout);
            this.InitialSize = values.GetFileSize(@"initial size", this.InitialSize);
            this.LimitSize = values.GetFileSize(@"limit size", this.LimitSize);
            this.Log = values.GetValue("log", this.Log);
            this.UtcDate = values.GetValue("utc", this.UtcDate);
        }
    }
}