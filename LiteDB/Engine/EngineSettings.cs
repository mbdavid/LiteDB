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
        /// Get/Set custom stream to be used as log file. If is null, use a new TempStream (for TempStrem datafile) or MemoryStream (for MemoryStream datafile)
        /// </summary>
        public Stream LogStream { get; set; } = null;

        /// <summary>
        /// Get/Set custom stream to be used as temp file. If is null, will create new FileStreamFactory with "-tmp" on name
        /// </summary>
        public Stream TempStream { get; set; } = null;

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
        /// When LOG file get are bigger than checkpoint size (in pages), do a soft checkpoint (and also do a checkpoint at shutdown)
        /// Checkpoint = 0 means no auto-checkpoint and no shutdown checkpoint
        /// </summary>
        public int Checkpoint { get; set; } = 1000;

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
        internal IStreamFactory CreateDataFactory()
        {
            if (this.DataStream != null)
            {
                return new StreamFactory(this.DataStream, this.Password);
            }
            else if (this.Filename == ":memory:")
            {
                return new StreamFactory(new MemoryStream(), this.Password);
            }
            else if (this.Filename == ":temp:")
            {
                return new StreamFactory(new TempStream(), this.Password);
            }
            else if (!string.IsNullOrEmpty(this.Filename))
            {
                return new FileStreamFactory(this.Filename, this.Password, this.ReadOnly);
            }

            throw new ArgumentException("EngineSettings must have Filename or DataStream as data source");
        }

        /// <summary>
        /// Create new IStreamFactory for logfile
        /// </summary>
        internal IStreamFactory CreateLogFactory()
        {
            if (this.LogStream != null)
            {
                return new StreamFactory(this.LogStream, this.Password);
            }
            else if (this.Filename == ":memory:")
            {
                return new StreamFactory(new MemoryStream(), this.Password);
            }
            else if (this.Filename == ":temp:")
            {
                return new StreamFactory(new TempStream(), this.Password);
            }
            else if (!string.IsNullOrEmpty(this.Filename))
            {
                var logName = FileHelper.GetLogFile(this.Filename);

                return new FileStreamFactory(logName, this.Password, this.ReadOnly);
            }

            return new StreamFactory(new MemoryStream(), this.Password);
        }

        /// <summary>
        /// Create new IStreamFactory for temporary file (sort)
        /// </summary>
        /// <returns></returns>
        internal IStreamFactory CreateTempFactory()
        {
            if (this.TempStream != null)
            {
                return new StreamFactory(this.TempStream, this.Password);
            }
            else if (this.Filename == ":memory:")
            {
                return new StreamFactory(new MemoryStream(), this.Password);
            }
            else if (this.Filename == ":temp:")
            {
                return new StreamFactory(new TempStream(), this.Password);
            }
            else if (!string.IsNullOrEmpty(this.Filename))
            {
                var tempName = FileHelper.GetTempFile(this.Filename);

                return new FileStreamFactory(tempName, this.Password, false);
            }

            return new StreamFactory(new TempStream(), this.Password);
        }
    }
}