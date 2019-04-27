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
            if (this.DbParam(parameterName) == value) return false;

            if (_locker.IsInTransaction) throw LiteException.AlreadyExistsTransaction();

            return this.AutoTransaction(transaction =>
            {
                // set parameter value
                switch (parameterName.ToUpper())
                {
                    case DB_PARAM_USERVERSION:
                        transaction.Pages.Commit += (h) =>
                        {
                            h.UserVersion = value;
                        };
                        break;
                    default: throw new LiteException(0, $"Unknow parameter name: {parameterName}");
                }

                return true;
            });
        }
    }
}