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
        public GroupByPipe(LiteEngine engine, TransactionService transaction, IDocumentLoader loader)
            : base(engine, transaction, loader)
        {
        }

        /// <summary>
        /// GroupBy Pipe Order
        /// - LoadDocument
        /// - IncludeBefore
        /// - Filter
        /// - OrderBy (to GroupBy)
        /// - OffSet
        /// - Limit
        /// - IncludeAfter
        /// - GroupBy
        /// - Having
        /// - SelectGroupBy
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
            else
            {
                // pipe: apply offset (no orderby)
                if (query.Offset > 0) source = source.Skip(query.Offset);

                // pipe: apply limit (no orderby)
                if (query.Limit < int.MaxValue) source = source.Take(query.Limit);
            }

            // do includes in result after filter
            foreach (var path in query.IncludeAfter)
            {
                source = this.Include(source, path);
            }

            // apply groupby
            var groups = this.GroupBy(source, query.GroupBy);

            // if contains having clause, run after select group by
            if (query.GroupBy.Having != null)
            {
                groups = this.Having(groups, query.GroupBy.Having);
            }

            // now, get only first document from each group
            return this.SelectGroupBy(groups, query.GroupBy.Select);
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

                    yield return new DocumentEnumerable(group);
                }
            }
        }

        private IEnumerable<BsonDocument> YieldDocuments(IEnumerator<BsonDocument> source, GroupBy groupBy, Done done)
        {
            var current = groupBy.Expression.ExecuteScalar(source.Current);

            groupBy.Select.Parameters["key"] = current;

            yield return source.Current;

            while (done.Running = source.MoveNext())
            {
                var key = groupBy.Expression.ExecuteScalar(source.Current);

                groupBy.Select.Parameters["key"] = current;

                if (key == current)
                {
                    yield return source.Current;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Transform groups of documents into single documents enumerable and apply select expression into group or return first document from each group
        /// </summary>
        private IEnumerable<BsonDocument> SelectGroupBy(IEnumerable<IEnumerable<BsonDocument>> groups, BsonExpression select)
        {
            var defaultName = select.DefaultFieldName();

            foreach (DocumentEnumerable group in groups)
            {
                // transfom group result if contains select expression
                var value = select.ExecuteScalar(group);

                if (value.IsDocument)
                {
                    yield return value.AsDocument;
                }
                else
                {
                    yield return new BsonDocument { [defaultName] = value };
                }

                group.Dispose();
            }
        }

        /// <summary>
        /// Pipe: Filter source using having bool expression to skip or include on final resultset
        /// </summary>
        protected IEnumerable<IEnumerable<BsonDocument>> Having(IEnumerable<IEnumerable<BsonDocument>> groups, BsonExpression having)
        {
            foreach(var source in groups)
            {
                var result = having.ExecuteScalar(source);

                if (result.IsBoolean && result.AsBoolean)
                {
                    yield return source;
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