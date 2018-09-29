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
        public QueryPipe(LiteEngine engine, TransactionService transaction, IDocumentLoader loader, CursorInfo cursor)
            : base(engine, transaction, loader, cursor)
        {
        }

        /// <summary>
        /// Query Pipe order
        /// - LoadDocument
        /// - IncludeBefore
        /// - Filter
        /// - IncludeAfter
        /// - Select
        /// - OrderBy
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

            // if is an aggregate query, run select transform over all resultset - will return a single value
            if (query.Select.All)
            {
                source = this.SelectAll(source, query.Select.Expression);
            }
            // run select transform in each document and return a new document or value
            else
            {
                source = this.Select(source, query.Select.Expression);
            }

            return source;
        }

        /// <summary>
        /// Pipe: Transaform final result appling expressin transform. Can return document or simple values
        /// </summary>
        private IEnumerable<BsonDocument> Select(IEnumerable<BsonDocument> source, BsonExpression select)
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
                var defaultName = select.DefaultFieldName();

                foreach (var doc in source)
                {
                    var result = select.Execute(doc, true);

                    foreach (var value in result)
                    {
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
            }
        }

        /// <summary>
        /// Pipe: Run select expression over all recordset
        /// </summary>
        private IEnumerable<BsonDocument> SelectAll(IEnumerable<BsonDocument> source, BsonExpression select)
        {
            var defaultName = select.DefaultFieldName();
            var result = select.Execute(source, true);

            foreach (var value in result)
            {
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
    }
}