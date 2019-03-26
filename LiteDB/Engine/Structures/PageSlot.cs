using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LiteDB.Engine
{
    /// <summary>
    /// Struct to represent a internal page data position - contains page byte position and data length size
    /// All variables are Int32 but are stored as UInt16 (2 bytes)
    /// </summary>
    [DebuggerStepThrough]
    internal struct PageSlot
    {
        /// <summary>
        /// Get page slot size: 2 bytes for Position + 2 bytes to Length (both UInt16)
        /// </summary>
        public const int SIZE = 4;

        /// <summary>
        /// Index slot in page
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Start buffer position
        /// </summary>
        public readonly int Position;

        /// <summary>
        /// Buffer length for this segment
        /// </summary>
        public readonly int Length;

        public PageSlot(int index, int position, int length)
        {
            this.Index = index;
            this.Position = position;
            this.Length = length;
        }
    }
}