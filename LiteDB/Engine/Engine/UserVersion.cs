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
                if (value == _header.UserVersion || _shutdown) return;

                // clone header to use in writer
                var confirm = _header.Clone();

                confirm.UserVersion = value;
                confirm.TransactionID = Guid.NewGuid();

                // create fake transaction with no pages to update (only confirm page)
                _wal.ConfirmTransaction(confirm, new PagePosition[0]);

                // update header instance
                _header.UserVersion = value;
            }
        }
    }
}