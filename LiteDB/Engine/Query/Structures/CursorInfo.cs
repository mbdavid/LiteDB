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
        /// Count time for run query
        /// </summary>
        public Stopwatch Timer { get; set; } = new Stopwatch();

        /// <summary>
        /// Get how many document query pipe read from disk/cache
        /// </summary>
        public int DocumentFetch { get; set; } = 0;

        /// <summary>
        /// Get how many documents returns on query
        /// </summary>
        public int DocumentResult { get; set; } = 0;

        /// <summary>
        /// Get/Set if current cursor are done (read all data)
        /// </summary>
        public bool Done { get; set; } = false;
    }
}