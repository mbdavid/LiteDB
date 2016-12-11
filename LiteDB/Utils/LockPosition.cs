using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Used to control lock state. Based on SQLite
    /// </summary>
    public struct LockPosition
    {
        public static LockPosition Empty = new LockPosition(0, 0);

        public long Position { get; set; }

        public long Length { get; set; }

        public LockPosition(long position, long length)
        {
            this.Position = position;
            this.Length = length;
        }
    }
}