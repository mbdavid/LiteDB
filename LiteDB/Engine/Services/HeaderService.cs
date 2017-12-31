using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class HeaderService
    {
        private TransactionService _trans;
        private Logger _log;

        public HeaderService(TransactionService trans, Logger log)
        {
            _trans = trans;
            _log = log;
        }
    }
}