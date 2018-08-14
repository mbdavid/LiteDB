using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LiteDB.Engine
{
    /// <summary>
    /// Store information about a single cursor running on query builder. Used in $open_cursors collection
    /// </summary>
    internal class CursorInfo
    {
        /// <summary>
        /// Get sequencial cursor ID
        /// </summary>
        public int CursorID { get; set; }

        /// <summary>
        /// Get transaction ID
        /// </summary>
        public Guid TransactionID { get; set; }

        /// <summary>
        /// Get snapshot read version
        /// </summary>
        public int ReadVersion { get; set; }

        /// <summary>
        /// Get collection name
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Get is collection are read/write lock mode
        /// </summary>
        public LockMode Mode { get; set; }

        /// <summary>
        /// Count time for run query
        /// </summary>
        public Stopwatch Timer { get; set; }

        /// <summary>
        /// Get how many document query pipe read from disk/cache
        /// </summary>
        public int DocumentLoad { get; set; } = 0;

        /// <summary>
        /// Get how many documents returns on query
        /// </summary>
        public int DocumentCount { get; set; } = 0;

        /// <summary>
        /// Get/Set if current cursor are done (read all data)
        /// </summary>
        public bool Done { get; set; } = false;

        /// <summary>
        /// Start timer
        /// </summary>
        public void Start()
        {
            this.Timer = Stopwatch.StartNew();
            this.Done = false;
        }

        /// <summary>
        /// Finish query results
        /// </summary>
        public void Finish()
        {
            this.Timer.Stop();
            this.Done = true;
        }

        /// <summary>
        /// Pause timer (to return to use document)
        /// </summary>
        public void Pause()
        {
            this.Timer.Stop();
        }

        /// <summary>
        /// Re-start timer
        /// </summary>
        public void Resume()
        {
            this.Timer.Start();
        }
    }
}