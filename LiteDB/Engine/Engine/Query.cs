using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Store all cursor information
        /// </summary>
        private LinkedList<CursorInfo> _cursors = new LinkedList<CursorInfo>();

        /// <summary>
        /// Run query over collection using a query definition
        /// </summary>
        public IBsonDataReader Query(string collection, QueryDefinition query)
        {
            if (string.IsNullOrWhiteSpace(collection)) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            IEnumerable<BsonDocument> source = null;

            // test if is an system collection
            if (collection.StartsWith("$"))
            {
                SqlParser.ParseCollection(new Tokenizer(collection), out var name, out var options);

                // get registered system collection to get data source
                var sys = this.GetSystemCollection(name);

                source = sys.Input(this, options);
                collection = sys.Name;
            }

            var exec = new QueryExecutor(this, collection, query, source);

            return exec.ExecuteQuery();
        }

        /// <summary>
        /// Create new cursor instance and add to cursor log. Start cursor timer
        /// </summary>
        internal CursorInfo NewCursor(TransactionService transaction, Snapshot snapshot)
        {
            lock(_cursors)
            {
                var cursor = new CursorInfo
                {
                    CursorID = _cursors.Count,
                    TransactionID = transaction.TransactionID,
                    CollectionName = snapshot.CollectionName,
                    Mode = snapshot.Mode
                };

                // add this new cursor on top of list
                _cursors.AddFirst(cursor);

                if (_cursors.Count > MAX_CURSOR_HISTORY)
                {
                    // remove 10% of old cursor from bottom when exceed max_cursor_history
                    for(var i = 0; i < ((double)MAX_CURSOR_HISTORY * .1d); i++)
                    {
                        _cursors.RemoveLast();
                    }
                }

                return cursor;
            }
        }
    }
}