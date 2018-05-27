using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
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
        /// Set database parameter value (this operation occurs without transaction)
        /// </summary>
        public bool SetParameter(string name, BsonValue value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            lock (_header)
            {
                // clone header to use in writer
                var confirm = _header.Clone() as HeaderPage;

                // if value == null, remove parameter
                if (value.IsNull)
                {
                    if (confirm.Parameters.TryRemove(name, out var dummy) == false)
                    {
                        return false;
                    }
                }
                else
                {
                    confirm.Parameters[name] = value;
                }

                confirm.TransactionID = Guid.NewGuid();

                // convert parameter into BsonDocument to calculate length
                var len = new BsonDocument(confirm.Parameters).GetBytesCount(false);

                if (len > HeaderPage.MAX_PARAMETERS_SIZE) throw LiteException.ParameterLimitExceeded(name);

                // create fake transaction with no pages to update (only header)
                _wal.ConfirmTransaction(confirm, new PagePosition[0]);

                // copy parameters from confirm page to header instance
                _header.Parameters = confirm.Parameters;

                return true;
            }
        }
    }
}