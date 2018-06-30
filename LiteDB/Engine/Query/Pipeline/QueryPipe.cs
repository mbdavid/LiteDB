using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Basic query pipe workflow - support filter, includes and orderby
    /// </summary>
    internal class QueryPipe : BasePipe
    {
        public QueryPipe(LiteEngine engine, TransactionService transaction, IDocumentLoader loader)
            : base(engine, transaction, loader)
        {
        }

        public override IEnumerable<BsonValue> Pipe(IEnumerable<IndexNode> nodes, QueryPlan query)
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

            if (query.OrderBy != null)
            {
                // pipe: orderby with offset+limit
                source = this.OrderBy(source, query.OrderBy.Expression, query.OrderBy.Order, query.Offset, query.Limit);
            }
            else
            {
                // pipe: apply offset (no orderby)
                if (query.Offset > 0) source = source.Skip(query.Offset);

                // pipe: apply limit (no orderby)
                if (query.Limit < int.MaxValue) source = source.Take(query.Limit);
            }

            // do includes in result before filter
            foreach (var path in query.IncludeAfter)
            {
                source = this.Include(source, path);
            }

            // if is an aggregate query, run select transform over all resultset - will return a single value
            if (query.Select?.Aggregate ?? false)
            {
                return query.Select.Expression.Execute(source, true);
            }

            // run select transform in each document result (if select == null, return source)
            return this.Select(source, query.Select?.Expression);
        }

        /// <summary>
        /// Pipe: Transaform final result appling expressin transform. Can return document or simple values
        /// </summary>
        private IEnumerable<BsonValue> Select(IEnumerable<BsonDocument> source, BsonExpression select)
        {
            if (select == null)
            {
                foreach (var value in source)
                {
                    yield return value;
                }
            }
            else
            {
                foreach (var doc in source)
                {
                    var result = select.Execute(doc, true);

                    foreach (var value in result)
                    {
                        yield return value;
                    }
                }
            }
        }
    }
}