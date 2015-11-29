using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Get database schema version
        /// </summary>
        public DbParams GetDbParams()
        {
            lock(_locker)
            {
                var header = _pager.GetPage<HeaderPage>(0);

                return header.DbParams;
            }
        }

        /// <summary>
        /// Set a new dbversion
        /// </summary>
        public void SetDbParams(DbParams dbparams)
        {
            lock (_locker)
            try
            {
                _transaction.Begin();

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
