using System;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Get database schema version
        /// </summary>
        public DbParams GetDbParam()
        {
            lock (_locker)
            {
                _transaction.AvoidDirtyRead();

                var header = _pager.GetPage<HeaderPage>(0);

                return header.DbParams;
            }
        }

        /// <summary>
        /// Set a new dbversion
        /// </summary>
        public void SetParam(DbParams dbparams)
        {
            lock (_locker)
            try
                {
                    _transaction.Begin();
                    _transaction.AvoidDirtyRead();

                    var header = _pager.GetPage<HeaderPage>(0);

                    header.DbParams = dbparams;

                    _pager.SetDirty(header);

                    _transaction.Commit();
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);
                    _transaction.Rollback();
                    throw;
                }
        }
    }
}