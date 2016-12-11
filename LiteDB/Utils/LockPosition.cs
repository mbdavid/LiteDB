using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Used to control lock state. Based on SQLite
    /// </summary>
    public struct LockPosition
    {
        private static Random _rnd = new Random();

        public static LockPosition Empty = new LockPosition(0, 0);

        public long Position { get; set; }

        public long Length { get; set; }

        public LockPosition(long position, long length)
        {
            this.Position = position;
            this.Length = length;
        }

        /// <summary>
        /// Returns a single lock position based on a random selection
        /// </summary>
        public static LockPosition Random(int position, int length)
        {
            var from = position;
            var to = from + length;

            return new LockPosition(_rnd.Next(from, to), 1);
        }
    }
}