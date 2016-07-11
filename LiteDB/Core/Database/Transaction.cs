using System;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        /// <summary>
        /// Begin a exclusive read/write transaction
        /// </summary>
        public Transaction BeginTrans()
        {
            return _engine.Value.BeginTrans();
        }
    }
}