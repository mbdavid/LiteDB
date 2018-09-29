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
        public GroupByPipe(LiteEngine engine, TransactionService transaction, IDocumentLoader loader, CursorInfo cursor)
            : base(engine, transaction, loader, cursor)
        {
        }

        /// <summary>
        /// GroupBy Pipe Order
        /// - LoadDocument
        /// - IncludeBefore
        /// - Filter
        /// - IncludeAfter
        /// - OrderBy to GroupBy
        /// - GroupBy
        /// - SelectGroupBy
        /// - Having
        /// - OffSet
        /// - Limit
        /// </summary>
        public override IEnumerable<BsonDocument> Pipe(IEnumerable<IndexNode> nodes, QueryPlan query)
        {
            // starts pipe loading document
            var source = this.LoadDocument(nodes, query.IsIndexKeyOnly, query.Fields.FirstOrDefault());

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

            // do includes in result after filter
            foreach (var path in query.IncludeAfter)
            {
                source = this.Include(source, path);
            }

            // pipe: if groupBy order is 0, do not need sort (already sorted by index)
            if (query.GroupBy.Order != 0)
            {
                source = this.OrderBy(source, query.GroupBy.Expression, query.GroupBy.Order, 0, int.MaxValue);
            }

            // apply groupby
            var groups = this.GroupBy(source, query.GroupBy.Expression);

            // now, get only first document from each group
            source = this.SelectGroupBy(groups, query.GroupBy.Select);

            // if contains having clause, run after select group by
            if (query.GroupBy.Having != null)
            {
                source = this.Having(source, query.GroupBy.Having);
            }

            // pipe: apply offset (no orderby)
            if (query.Offset > 0) source = source.Skip(query.Offset);

            // pipe: apply limit (no orderby)
            if (query.Limit < int.MaxValue) source = source.Take(query.Limit);

            // return document pipe
            return source;
        }

        /// <summary>
        /// Apply groupBy expression and transform results
        /// </summary>
        private IEnumerable<IEnumerable<BsonDocument>> GroupBy(IEnumerable<BsonDocument> source, BsonExpression expr)
        {
            using (var enumerator = source.GetEnumerator())
            {
                var done = new Done { Running = enumerator.MoveNext() };

                while (done.Running)
                {
                    var group = YieldDocuments(enumerator, expr, done);

                    yield return new DocumentEnumerable(group, _loader);
                }
            }
        }

        private IEnumerable<BsonDocument> YieldDocuments(IEnumerator<BsonDocument> source, BsonExpression expr, Done done)
        {
            var current = expr.Execute(source.Current, true).First();

            yield return source.Current;

            while (done.Running = source.MoveNext())
            {
                var key = expr.Execute(source.Current, true).First();

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
            var defaultName = select?.DefaultFieldName();

            foreach (DocumentEnumerable group in groups)
            {
                // transfom group result if contains select expression
                if (select != null)
                {
                    var result = select.Execute(group, true);

                    // each group result must return a single value after run expression
                    var value = result.First();

                    if (value.IsDocument)
                    {
                        yield return value.AsDocument;
                    }
                    else
                    {
                        yield return new BsonDocument { [defaultName] = value };
                    }
                }
                else
                {
                    // get first document BUT with full source scan (get from DocumentEnumerable)
                    var doc = group.FirstOrDefault();

                    yield return doc;
                }

                group.Dispose();
            }
        }

        /// <summary>
        /// Pipe: Filter source using having bool expression to skip or include on final resultset
        /// </summary>
        protected IEnumerable<BsonDocument> Having(IEnumerable<BsonDocument> source, BsonExpression having)
        {
            foreach (var doc in source)
            {
                var result = having.Execute(doc, true).First();

                if (result.IsBoolean && result.AsBoolean)
                {
                    yield return doc;
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