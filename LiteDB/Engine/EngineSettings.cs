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
    public class EngineSettings : IEngineSettings
    {
        /// <summary>
        /// Get/Set custom stream to be used as datafile (can be MemoryStrem or TempStream). Do not use FileStream - to use physical file, use "filename" attribute (and keep DataStrem/WalStream null)
        /// </summary>
        public Stream DataStream { get; set; } = null;

        /// <summary>
        /// Get/Set custom stream to be used as walfile. If is null, use a new TempStream (for TempStrem datafile) or MemoryStrema (for MemoryStream datafile)
        /// </summary>
        public Stream WalStream { get; set; } = null;

        /// <summary>
        /// Get/Set custom instance for Logger
        /// </summary>
        public Logger Log { get; set; } = null;

        /// <summary>
        /// Full path or relative path from DLL directory. Can use ':temp:' for temp database or ':memory:' for in-memory database. (default: ':memory:')
        /// </summary>
        public string Filename { get; set; } = ":memory:";

        /// <summary>
        /// Timeout for waiting unlock operations (default: 1 minute)
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// If database is new, initialize with allocated space (in bytes) (default: 0)
        /// </summary>
        public long InitialSize { get; set; } = 0;

        /// <summary>
        /// Max limit of datafile (in bytes) (default: MaxValue)
        /// </summary>
        public long LimitSize { get; set; } = long.MaxValue;

        /// <summary>
        /// Debug messages from database - (default: Logger.NONE)
        /// </summary>
        public byte LogLevel { get; set; } = Logger.NONE;

        /// <summary>
        /// Returns date in UTC timezone from BSON deserialization (default: false == LocalTime)
        /// </summary>
        public bool UtcDate { get; set; } = false;

        /// <summary>
        /// When wal file get this checkpoint limit, write over data disk
        /// </summary>
        public int Checkpoint { get; set; } = 1000;

        /// <summary>
        /// Indicate that engine will do a checkpoint on dispose database
        /// </summary>
        public bool CheckpointOnShutdown { get; set; } = true;

        /// <summary>
        /// Define max pages a trasaction must keep in-memory before flush to WAL file. Must be larger than 100 (default 1000)
        /// </summary>
        public int MaxMemoryTransactionSize { get; set; } = 1000;

        /// <summary>
        /// Get datafile factory
        /// </summary>
        public IDiskFactory GetDiskFactory()
        {
            if (this.Filename == ":memory:")
            {
                return new StreamDiskFactory(new MemoryStream(), this.WalStream ?? new MemoryStream());
            }
            else if (this.Filename == ":temp:")
            {
                return new StreamDiskFactory(new TempStream(), this.WalStream ?? new TempStream());
            }
            else if(!string.IsNullOrEmpty(this.Filename))
            {
                return new FileStreamDiskFactory(this.Filename);
            }
            else
            {
                if (this.DataStream == null) throw new ArgumentException("EngineSettings must have Filename or DataStream as data source");

                return new StreamDiskFactory(this.DataStream, this.WalStream ?? new TempStream());
            }
        }
    }
}