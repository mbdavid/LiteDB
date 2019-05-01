using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement query using GroupBy expression
    /// </summary>
    internal class GroupByPipe : BasePipe
    {
        public GroupByPipe(LiteEngine engine, TransactionService transaction, IDocumentLookup loader)
            : base(engine, transaction, loader)
        {
        }

        /// <summary>
        /// GroupBy Pipe Order
        /// - LoadDocument
        /// - IncludeBefore
        /// - Filter
        /// - OrderBy (to GroupBy)
        /// - IncludeAfter
        /// - GroupBy
        /// - HavingSelectGroupBy
        /// - OffSet
        /// - Limit
        /// </summary>
        public override IEnumerable<BsonDocument> Pipe(IEnumerable<IndexNode> nodes, QueryPlan query)
        {
            // starts pipe loading document
            var source = this.LoadDocument(nodes);

            // do includes in result before filter
            foreach (var path in query.IncludeBefore)
            {
                source = this.Include(source, path);
            }

            // filter results according expressions
            foreach (var expr in query.Filters)
            {
                source = this.Filter(source, expr);
            }

            // pipe: orderBy used in GroupBy
            if (query.OrderBy != null)
            {
                source = this.OrderBy(source, query.OrderBy.Expression, query.OrderBy.Order, 0, int.MaxValue);
            }

            // do includes in result after filter
            foreach (var path in query.IncludeAfter)
            {
                source = this.Include(source, path);
            }

            // apply groupby
            var groups = this.GroupBy(source, query.GroupBy);

            // now, get only first document from each group
            var result = this.SelectGroupBy(groups, query.GroupBy);

            // pipe: apply offset
            if (query.Offset > 0) result = result.Skip(query.Offset);

            // pipe: apply limit
            if (query.Limit < int.MaxValue) result = result.Take(query.Limit);

            return result;
        }

        /// <summary>
        /// Apply groupBy expression and transform results
        /// </summary>
        private IEnumerable<IEnumerable<BsonDocument>> GroupBy(IEnumerable<BsonDocument> source, GroupBy groupBy)
        {
            using (var enumerator = source.GetEnumerator())
            {
                var done = new Done { Running = enumerator.MoveNext() };

                while (done.Running)
                {
                    var group = YieldDocuments(enumerator, groupBy, done);

                    yield return new DocumentGroup(groupBy.Select.Parameters["key"], enumerator.Current, group, _lookup);
                }
            }
        }

        /// <summary>
        /// YieldDocuments will run over all key-ordered source and returns groups of source
        /// </summary>
        private IEnumerable<BsonDocument> YieldDocuments(IEnumerator<BsonDocument> enumerator, GroupBy groupBy, Done done)
        {
            var current = groupBy.Expression.ExecuteScalar(enumerator.Current);

            groupBy.Select.Parameters["key"] = current;

            yield return enumerator.Current;

            while (done.Running = enumerator.MoveNext())
            {
                var key = groupBy.Expression.ExecuteScalar(enumerator.Current);

                if (key == current)
                {
                    // yield return document in same key (group)
                    yield return enumerator.Current;
                }
                else
                {
                    // stop current sequence
                    yield break;
                }
            }
        }

        /// <summary>
        /// Run Select expression over a group source - each group will return a single value
        /// If contains Having expression, test if result = true before run Select
        /// </summary>
        private IEnumerable<BsonDocument> SelectGroupBy(IEnumerable<IEnumerable<BsonDocument>> groups, GroupBy groupBy)
        {
            var defaultName = groupBy.Select.DefaultFieldName();

            foreach (DocumentGroup group in groups)
            {
                // transfom group result if contains select expression
                BsonValue value;

                try
                {
                    if (groupBy.Having != null)
                    {
                        var filter = groupBy.Having.ExecuteScalar(group, group.Root, group.Root);

                        if (!filter.IsBoolean || !filter.AsBoolean) continue;
                    }

                    value = groupBy.Select.ExecuteScalar(group, group.Root, group.Root);
                }
                finally
                {
                    group.Dispose();
                }

                if (value.IsDocument)
                {
                    yield return value.AsDocument;
                }
                else
                {
                    yield return new BsonDocument { [defaultName] = value };
                }
            }
        }

        /// <summary>
        /// Bool inside a class to be used as "ref" parameter on ienumerable
        /// </summary>
        private class Done
        {
            public bool Running = false;
        }
    }
}