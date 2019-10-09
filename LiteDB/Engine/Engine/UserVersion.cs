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
        /// Get/Set database user version	
        /// </summary>	
        public int UserVersion
        {
            get
            {
                return _header.UserVersion;
            }
            set
            {
                if (_header.UserVersion == value) return;

                if (_locker.IsInTransaction) throw LiteException.AlreadyExistsTransaction();

                LOG($"change userVersion to `{value}`", "COMMAND");

                // do a inside transaction to edit UserVersion on commit event	
                this.AutoTransaction(transaction =>
                {
                    transaction.Pages.Commit += (h) =>
                    {
                        h.UserVersion = value;
                    };

                    return true;
                });
            }
        }
    }
}