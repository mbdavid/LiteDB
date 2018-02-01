using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class to pipe documents and apply Load/Filter/Includes/OrderBy commands
    /// </summary>
    internal class QueryPipe
    {
        /// <summary>
        /// Start pipe documents process
        /// </summary>
        public IEnumerable<BsonDocument> Pipe(IEnumerable<BsonDocument> source, Query query)
        {
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

            // implement orderby list
            foreach(var orderBy in query.OrderBy)
            {
                source = this.OrderBy(source, orderBy, query.Limit);
            }

            // do includes in result before filter
            foreach (var path in query.IncludeAfter)
            {
                source = this.Include(source, path);
            }

            // transfom result if contains select expression
            if (query.Select != null)
            {
                source = this.Select(source, query.Select);
            }

            // pipe: apply offset
            if (query.Offset > 0) source = source.Skip(query.Offset);

            // pipe: apply limit
            if (query.Limit < int.MaxValue) source = source.Take(query.Limit);

            // return document pipe
            return source;
        }

        private IEnumerable<BsonDocument> OrderBy(IEnumerable<BsonDocument> source, OrderBy orderBy, int limit)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Pipe: Do include in result document according path expression
        /// </summary>
        private IEnumerable<BsonDocument> Include(IEnumerable<BsonDocument> source, BsonExpression path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Pipe: Filter document according expression. Expression must be an Bool result
        /// </summary>
        private IEnumerable<BsonDocument> Filter(IEnumerable<BsonDocument> source, BsonExpression expr)
        {
            foreach(var doc in source)
            {
                var result = expr.Execute(doc, true).FirstOrDefault();

                // expression must return an boolean and be true to return document
                if (result.IsBoolean && result.AsBoolean == true)
                {
                    yield return doc;
                }
            }
        }

        /// <summary>
        /// Pipe: OrderBy documents according orderby expression/order
        /// </summary>
        private IEnumerable<BsonDocument> OrderBy(IEnumerable<BsonDocument> source, OrderBy orderBy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Pipe: Transaform final result appling expressin transform. Expression must return an BsonDocument (or will be converter into a new documnet)
        /// </summary>
        private IEnumerable<BsonDocument> Select(IEnumerable<BsonDocument> source, BsonExpression expr)
        {
            foreach(var doc in source)
            {
                var result = expr.Execute(doc, true);

                var value = result.First();

                if (value.IsDocument)
                {
                    yield return value.AsDocument;
                }
                else
                {
                    yield return new BsonDocument { ["expr"] = value };
                }
            }
        }
    }
}