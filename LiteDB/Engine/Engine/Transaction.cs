using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine : IDisposable
    {
        private Stack<LockControl> _transactions = new Stack<LockControl>();

        /// <summary>
        /// Starts a new transaction keeping all changed from now in memory only until Commit() be executed.
        /// </summary>
        public void BeginTrans()
        {
            if (_autocommit == false) throw new NotSupportedException("Transaction are not supported when AutoCommit is False");

            _transactions.Push(_locker.Write());
        }

        /// <summary>
        /// Persist in disk all changed from last BeginTrans()
        /// </summary>
        public void Commit()
        {
            // only do "real commit" if is last transaction in stack or if autocommit = false
            if (_transactions.Count == 1)
            {
                _trans.Commit();
            }
            else if (_autocommit == false)
            {
                // for autocommit must lock datafile
                using (_locker.Write())
                {
                    _trans.Commit();
                }
            }

            // if contains transactions on stack, remove top and dispose (only last transaction will release lock)
            if (_transactions.Count > 0)
            {
                _transactions.Pop().Dispose();
            }
        }

        /// <summary>
        /// Discard all changes from last BeginTrans()
        /// </summary>
        public void Rollback()
        {
            _trans.Rollback();

            while(_transactions.Count > 0)
            {
                _transactions.Pop().Dispose();
            }
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

                    // when autocommit is false, transaction count is always 0
                    if (_transactions.Count == 0 && _autocommit == true) _trans.Commit();

                    return result;
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);

                    // if an error occurs during an operation, rollback must be called to avoid datafile inconsistent
                    this.Rollback();

                    throw;
                }
            }
        }
    }
}