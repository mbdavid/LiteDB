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
        private int _bufferSize;
        private int _counter;
        private int _skip;
        private int _initialSkip;
        private int _limit;
        private Query _query;

        public int Skip { get { return _skip; } }
        public IEnumerator<IndexNode> Nodes { get; set; }
        public bool HasMore { get; private set; }
        public int Total { get { return _counter; } }

        public QueryContext(Query query, int skip, int limit, int bufferSize)
        {
            _query = query;
            _skip = _initialSkip = skip;
            _limit = limit;
            _bufferSize = bufferSize;
            _counter = 0;

            this.HasMore = true;
            this.Nodes = null;
        }

        public IEnumerable<BsonDocument> GetDocuments(TransactionService trans, DataService data, Logger log)
        {
            var index = _bufferSize;

            while (index > 0)
            {
                // checks if cache are full
                trans.CheckPoint();

                // read next node
                this.HasMore = this.Nodes.MoveNext();

                // if finish, exit loop
                if (this.HasMore == false) yield break;

                // if run under index, skip/limit before deserialize
                if (_query.UseIndex)
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

                index--;

                // increment counter document
                _counter++;

                // avoid lock again just to check limit
                if (_limit == 0)
                {
                    this.HasMore = false;
                }

                yield return doc;
            }

            // for next run, must skip counter because do continue after last
            _skip = _counter + _initialSkip;
        }

        public IEnumerable<BsonValue> GetIndexKeys(TransactionService trans, Logger log)
        {
            var index = _bufferSize;

            while (index > 0)
            {
                trans.CheckPoint();

                // read next node
                this.HasMore = this.Nodes.MoveNext();

                // skip N nodes 
                if (--_skip >= 0) continue;

                if (this.HasMore == false) yield break;

                // and limit in N max 
                if (--_limit <= -1)
                {
                    this.HasMore = false;
                    yield break;
                }

                index--;

                // increment counter key
                _counter++;

                // avoid lock again just to check limit
                if (_limit == 0) this.HasMore = false;

                yield return this.Nodes.Current.Key;
            }

            // for next run, must skip counter because do continue after last
            _skip = _counter + _initialSkip;
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