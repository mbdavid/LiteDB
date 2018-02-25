using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Get database parameter value - if not exists, return default value
        /// </summary>
        public BsonValue GetParameter(string name, BsonValue defaultValue)
        {
            if (_header.Parameters.TryGetValue(name, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Set database parameter value
        /// </summary>
        public void SetParameter(string name, BsonValue value)
        {
            lock (_header)
            {
                // clone header to use in writer
                var confirm = _header.Clone() as HeaderPage;

                confirm.Parameters[name] = value;
                confirm.TransactionID = Guid.NewGuid();

                // convert parameter into BsonDocument to calculate length
                var len = new BsonDocument(confirm.Parameters).GetBytesCount(false);

                if (len > HeaderPage.MAX_PARAMETERS_SIZE) throw LiteException.ParameterLimitExceeded(name);

                // create fake transaction with no pages to update (only header)
                _wal.ConfirmTransaction(confirm, new PagePosition[0]);

                // update header instance
                _header.Parameters[name] = value;
            }
        }
    }
}