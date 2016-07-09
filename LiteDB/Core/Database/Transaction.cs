using System;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        /// <summary>
        /// Begin a exclusive read/write transaction
        /// </summary>
        public void BeginTrans()
        {
            _engine.Value.BeginTrans();
        }

        /// <summary>
        /// Persist all changes on disk. Always use this method to finish your changes on database
        /// </summary>
        public void Commit()
        {
            _engine.Value.Commit();
        }

        /// <summary>
        /// Cancel all write operations and keep datafile as is before BeginTrans() called.
        /// Rollback are implicit on a database operation error, so you do not need call for database errors (only on business rules).
        /// </summary>
        public void Rollback()
        {
            _engine.Value.Rollback();
        }
    }
}