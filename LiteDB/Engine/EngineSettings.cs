using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// All engine settings used to starts new engine
    /// </summary>
    public class EngineSettings
    {
        /// <summary>
        /// Get/Set custom stream to be used as datafile (can be MemoryStrem or TempStream). Do not use FileStream - to use physical file, use "filename" attribute (and keep DataStrem/WalStream null)
        /// </summary>
        public Stream DataStream { get; set; } = null;

        /// <summary>
        /// Get/Set custom stream to be used as log file. If is null, use a new TempStream (for TempStrem datafile) or MemoryStrema (for MemoryStream datafile)
        /// </summary>
        public Stream LogStream { get; set; } = null;

        /// <summary>
        /// Full path or relative path from DLL directory. Can use ':temp:' for temp database or ':memory:' for in-memory database. (default: null)
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Get database password to decrypt pages
        /// </summary>
        public string Password { get; set; }

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
        /// Returns date in UTC timezone from BSON deserialization (default: false == LocalTime)
        /// </summary>
        public bool UtcDate { get; set; } = false;

        /// <summary>
        /// Indicate that engine will open files in readonly mode (and will not support any database change)
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// Size, in PAGES, for each buffer array (used in MemoryStore) - Each byte array will be created with this size * PAGE_SIZE. 
        /// Should be > 100 (800kb) - Default: 1000 (8Mb each segment)
        /// </summary>
        public int MemorySegmentSize { get; set; } = MEMORY_SEGMENT_SIZE;

        /// <summary>
        /// Define, in page size, how many pages each collection will keep in memory before flush to disk. When reach this size
        /// all dirty pages will be saved on log files and clean pages will be removed from cache
        /// </summary>
        public int MaxTransactionSize { get; set; } = MAX_TRANSACTION_SIZE;

        /// <summary>
        /// Create new IStreamFactory for datafile
        /// </summary>
        internal IStreamFactory CreateDataFactory() => this.CreateStreamFactory(true);

        /// <summary>
        /// Create new IStreamFactory for logfile
        /// </summary>
        internal IStreamFactory CreateLogFactory() => this.CreateStreamFactory(false);

        /// <summary>
        /// Get Data/Log Stream factory
        /// </summary>
        private IStreamFactory CreateStreamFactory(bool dataFile)
        {
            var stream = dataFile ? this.DataStream : this.LogStream;
            var filename = dataFile ? this.Filename : FileHelper.GetLogFile(this.Filename);

            if (stream != null)
            {
                return new StreamFactory(stream);
            }
            else if (filename == ":memory:")
            {
                return new StreamFactory(new MemoryStream());
            }
            else if (filename == ":temp:")
            {
                return new StreamFactory(new TempStream());
            }
            else if (!string.IsNullOrEmpty(filename))
            {
                return new FileStreamFactory(filename, this.ReadOnly);
            }

            throw new ArgumentException("EngineSettings must have Filename or DataStream as data source");
        }
    }
}