using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB.Engine
{
    /// <summary>
    /// All engine settings used to starts new engine
    /// </summary>
    public class EngineSettings
    {
        /// <summary>
        /// Get/Set custom stream to be used as datafile (can be MemoryStrem or TempStream). To use physical file, use "filename" attribute
        /// </summary>
        public Stream DataStream { get; set; } = null;

        /// <summary>
        /// Get/Set custom instance for Logger
        /// </summary>
        public Logger Log { get; set; } = null;

        /// <summary>
        /// "filename": Full path or relative path from DLL directory (default: ':memory:')
        /// </summary>
        public string Filename { get; set; } = ":memory:";

        /// <summary>
        /// "timeout": Timeout for waiting unlock operations (default: 1 minute)
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// "read only": Define if datafile will be read only, with no insert/update/delete data (default: false)
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// "initial size": If database is new, initialize with allocated space - support KB, MB, GB (default: null)
        /// </summary>
        public long InitialSize { get; set; } = 0;

        /// <summary>
        /// "limit size": Max limit of datafile - support KB, MB, GB (default: null)
        /// </summary>
        public long LimitSize { get; set; } = long.MaxValue;

        /// <summary>
        /// "log": Debug messages from database - use `LiteDatabase.Log` (default: Logger.NONE)
        /// </summary>
        public byte LogLevel { get; set; } = Logger.NONE;

        /// <summary>
        /// "utc": Returns date in UTC timezone from BSON deserialization (default: false == LocalTime)
        /// </summary>
        public bool UtcDate { get; set; } = false;

        /// <summary>
        /// "syncOverAsync": Use "sync over async" to UWP apps access any directory
        /// </summary>
        public bool SyncOverAsync { get; set; } = false;

        /// <summary>
        /// "checkpoint": When wal file get this checkpoint limit, write over data disk
        /// </summary>
        public int Checkpoint { get; set; } = 1000;

        /// <summary>
        /// Get datafile factory
        /// </summary>
        internal IDiskFactory GetDiskFactory()
        {
            if (this.DataStream != null)
            {
                return new StreamDiskFactory(this.DataStream, new TempStream());
            }
            if (this.Filename == ":memory:")
            {
                return new StreamDiskFactory(new MemoryStream(), new MemoryStream());
            }
            else if (this.Filename == ":temp:")
            {
                return new StreamDiskFactory(new TempStream(), new TempStream());
            }
            else
            {
                return new FileStreamDiskFactory(this.Filename, this.ReadOnly, this.SyncOverAsync);
            }
        }
    }
}