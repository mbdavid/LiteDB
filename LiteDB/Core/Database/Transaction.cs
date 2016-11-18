using System;

namespace LiteDB
{
    public partial class LiteDatabase
    {
        /// <summary>
        /// Begin a exclusive read/write transaction
        /// </summary>
        public LiteTransaction BeginTrans()
        {
            return _engine.Value.BeginTrans();
        }
    }
}