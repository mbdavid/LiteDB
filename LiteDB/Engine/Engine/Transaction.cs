using System;

namespace LiteDB
{
    public partial class LiteEngine : IDisposable
    {
        /// <summary>
        /// Nested transaction counter
        /// </summary>
        private int _nested = 0;

        /// <summary>
        /// Begins a new exclusive transaction (support nested transactions)
        /// </summary>
        public void BeginTrans()
        {
            _locker.BeginExclusiveLock();
            _nested++;
        }

        /// <summary>
        /// Commit current transaction
        /// </summary>
        public void Commit()
        {
            if (--_nested <= 0) _trans.Commit();
            _locker.ExitExclusiveLock();
        }

        /// <summary>
        /// Rollback current transaction and restore initial database state
        /// </summary>
        public void Rollback()
        {
            _trans.Rollback();
            _nested = 0;
            _locker.ExitExclusiveLock(true);
        }

        /// <summary>
        /// Encapsulate all write transaction operation
        /// </summary>
        private T Transaction<T>(string colName, bool addIfNotExists, Func<CollectionPage, T> action)
        {
            using(_locker.Write())
            {
                try
                {
                    var col = this.GetCollectionPage(colName, addIfNotExists);

                    var result = action(col);

                    // commit only if there is no nested transaction
                    if (_nested == 0) _trans.Commit();

                    return result;
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);
                    _trans.Rollback();
                    _nested = 0;
                    throw;
                }
            }
        }
    }
}