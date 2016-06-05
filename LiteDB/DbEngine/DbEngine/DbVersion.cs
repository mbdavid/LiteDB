using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Read DbVersion variable from header page
        /// </summary>
        public ushort ReadDbVersion()
        {
            try
            {
                // initalize read only transaction
                _transaction.Begin(true);

                var header = _pager.GetPage<HeaderPage>(0);

                // complete transaction, release datafile
                _transaction.Complete();

                return header.DbVersion;
            }
            catch (Exception ex)
            {
                _log.Write(Logger.ERROR, ex.Message);
                _transaction.Abort();
                throw;
            }
        }

        /// <summary>
        /// Write DbVersion variable in header page
        /// </summary>
        public void WriteDbVersion(ushort version)
        {
            try
            {
                // initalize read/write transaction
                _transaction.Begin(false);

                var header = _pager.GetPage<HeaderPage>(0);

                header.DbVersion = version;

                _pager.SetDirty(header);

                // complete transaction, release datafile
                _transaction.Complete();
            }
            catch (Exception ex)
            {
                _log.Write(Logger.ERROR, ex.Message);
                _transaction.Abort();
                throw;
            }
        }
    }
}