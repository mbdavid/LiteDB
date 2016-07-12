using System;

namespace LiteDB
{
    public partial class LiteDatabase
    {
        /// <summary>
        /// Begin a exclusive read/write transaction
        /// </summary>
        public void BeginTrans()
        {
            _engine.Value.BeginTrans();
        }
    }
}