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
        /// Get engine internal parameters by parameter name
        /// </summary>
        public BsonValue DbParam(string name)
        {
            switch (name.ToUpperInvariant())
            {
                case "USER_VERSION": return _header.UserVersion;
                case "CULTURE": return _header.Collation.Culture.Name;
                case "LCID": return _header.Collation.LCID;
                case "SORT": return (int)_header.Collation.CompareOptions;
                case "TIMEOUT": return (int)_settings.Timeout.TotalSeconds;
                case "UTC_DATE": return _settings.UtcDate;
                case "READ_ONLY": return _settings.ReadOnly;
            }

            throw new LiteException(0, $"Invalid database parameter: `{name}`");
        }

        /// <summary>
        /// Set engine parameter (when parameter are writable)
        /// </summary>
        public bool DbParam(string name, BsonValue value)
        {
            if (this.DbParam(name) == value) return false;

            if (_locker.IsInTransaction) throw LiteException.AlreadyExistsTransaction();

            // do a inside transaction to edit UserVersion on commit event	
            return this.AutoTransaction(transaction =>
            {
                switch (name.ToUpperInvariant())
                {
                    case "USER_VERSION":
                        transaction.Pages.Commit += (h) =>
                        {
                            h.UserVersion = value.AsInt32;
                        };
                        break;
                    case "CULTURE": 
                    case "LCID":
                    case "SORT":
                    case "TIMEOUT":
                    case "UTC_DATE":
                    case "READ_ONLY": throw new LiteException(0, $"Parameter {name} are read-only");
                    default: throw new LiteException(0, $"Invalid database parameter: `{name}`");
                }

                return true;
            });
        }
    }
}