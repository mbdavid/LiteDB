using System;

namespace LiteDB
{
   public partial class DbEngine
    {
        /// <summary>
        /// Read DbVersion variable from header page
        /// </summary>
        public ushort ReadDbVersion()
        {
            // initalize read only transaction
            using (var trans = _transaction.Begin(true))
            {
                try
                {

                    var header = _pager.GetPage<HeaderPage>(0);

                    // complete transaction, release datafile
                    trans.Commit();

                    return header.DbVersion;
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);
                    trans.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Write DbVersion variable in header page
        /// </summary>
        public void WriteDbVersion(ushort version)
        {
            // initalize read/write transaction
            using (var trans = _transaction.Begin(false))
            {
                try
                {

                    var header = _pager.GetPage<HeaderPage>(0);

                    header.DbVersion = version;

                    _pager.SetDirty(header);

                    // complete transaction, release datafile
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);
                    trans.Rollback();
                    throw;
                }
            }
        }
    }
}