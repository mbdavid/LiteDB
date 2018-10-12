using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Get database header parameter value
        /// </summary>
        public BsonValue DbParam(string parameterName)
        {
            switch(parameterName.ToUpper())
            {
                case DB_PARAM_USERVERSION: return _header.UserVersion;
                default: throw new LiteException(0, $"Unknow parameter name: {parameterName}");
            }
        }

        /// <summary>
        /// Set new database parameter value. Requires no transaction at this time
        /// </summary>
        public bool DbParam(string parameterName, BsonValue value)
        {
            // check if same value or database are in shutdown mode
            if (this.DbParam(parameterName) == value || _shutdown) return false;

            if (_locker.IsInTransaction) throw LiteException.InvalidTransactionState("DbParam", TransactionState.Active);

            // simle "lock (_header)" was modified to enter all database in reserved lock to check database readonly mode
            _locker.EnterReserved(false);

            try
            {
                // clone header to use in writer
                var header = _header.Clone();

                // set parameter value
                switch (parameterName.ToUpper())
                {
                    case DB_PARAM_USERVERSION:
                        header.UserVersion = value;
                        break;
                    default: throw new LiteException(0, $"Unknow parameter name: {parameterName}");
                }

                header.TransactionID = ObjectId.NewObjectId();
                header.IsConfirmed = true;
                header.IsDirty = true;

                var positions = new Dictionary<uint, PagePosition>();

                _wal.LogFile.WritePages(new[] { header }, positions);

                // create fake transaction with no pages to update (only confirm page)
                _wal.ConfirmTransaction(header.TransactionID, positions.Values);

                // update header instance
                _header.UserVersion = value;

                return true;
            }
            finally
            {
                _locker.ExitReserved(false);
            }
        }
    }
}