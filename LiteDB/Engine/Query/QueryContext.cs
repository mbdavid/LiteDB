using System;
using System.Collections;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Include all components to be used in execution of a qery
    /// </summary>
    internal class QueryContext : IDisposable
    {
        private int _position;
        private int _skip;
        private int _limit;
        private Query _query;

        public int Skip { get { return _skip; } set { _skip = value; } }
        public IEnumerator<IndexNode> Nodes { get; set; }
        public bool HasMore { get; private set; }
        public int Position { get { return _position; } }

        public QueryContext(Query query, int skip, int limit)
        {
            _query = query;
            _skip = skip;
            _limit = limit;
            _position = skip;

            this.HasMore = true;
            this.Nodes = null;
        }

        public IEnumerable<BsonDocument> GetDocuments(TransactionService trans, DataService data, Logger log)
        {
            if (_skip > 0)
            {
                log.Write(Logger.QUERY, "skiping {0} documents", _skip);
            }

            // while until must cache not recycle
            while (trans.CheckPoint() == false)
            {
                // read next node
                this.HasMore = this.Nodes.MoveNext();

                // if finish, exit loop
                if (this.HasMore == false) yield break;

                // if run ONLY under index, skip/limit before deserialize
                if (_query.UseIndex && _query.UseFilter == false)
                {
                    if (--_skip >= 0) continue;

                    if (--_limit <= -1)
                    {
                        this.HasMore = false;
                        yield break;
                    }
                }

                // get current node
                var node = this.Nodes.Current;

                // read document from data block
                var buffer = data.Read(node.DataBlock);
                var doc = BsonSerializer.Deserialize(buffer).AsDocument;

                // if need run in full scan, execute full scan and test return
                if (_query.UseFilter)
                {
                    // execute query condition here - if false, do not add on final results
                    if (_query.FilterDocument(doc) == false) continue;

                    // implement skip/limit after deserialize in full scan
                    if (--_skip >= 0) continue;

                    if (--_limit <= -1)
                    {
                        this.HasMore = false;
                        yield break;
                    }
                }

                // increment position cursor
                _position++;

                // avoid lock again just to check limit
                if (_limit == 0)
                {
                    this.HasMore = false;
                }

                yield return doc;
            }
        }

        public void Dispose()
        {
            if (this.Nodes != null)
            {
                this.Nodes.Dispose();
            }
        }
    }
}