using System;
using System.Collections.Generic;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Simple parameter class to be passed into IEnumerable classes loop ("ref" do not works)
    /// </summary>
    internal class FlipCoin
    {
        private readonly Random _rnd;

        public FlipCoin(int? seed)
        {
            _rnd = seed == null ? new Random() : new Random(seed.Value);
        }

        /// <summary>
        /// Flip coin - skip list - returns level node (start in 1)
        /// </summary>
        public byte Flip()
        {
            byte level = 1;
            for (int R = _rnd.Next(); (R & 1) == 1; R >>= 1)
            {
                level++;
                if (level == MAX_LEVEL_LENGTH) break;
            }
            return level;
        }
    }
}