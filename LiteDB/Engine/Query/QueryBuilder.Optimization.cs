using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class to provider a fluent query API to complex queries. This class will be optimied to convert into Query class before run
    /// </summary>
    public partial class QueryBuilder
    {
        /// <summary>
        /// Minimum number of document in collection to try to create index - below this, use full scan because it's fast than index
        /// </summary>
        private const int DOCUMENT_COUNT_TO_CREATE_INDEX = 100;

        /// <summary>
        /// Indicate this query already optimized
        /// </summary>
        private bool _optimized = false;

        /// <summary>
        /// Fill QueryPlan instance (_query)
        /// - Select best index option (or create new one)
        /// - Fill includes 
        /// - Set orderBy
        /// </summary>
        private void OptimizeQuery(Snapshot snapshot)
        {
            // if already has estimate cost, no need optimze
            if (_optimized) return;

            // define index (can create if needed)
            this.DefineIndex(snapshot);

            //TODO do smart choices
            _query.IncludeAfter.AddRange(_includes);
            _query.Order = _order;
            _query.OrderBy = _orderBy;

            _optimized = true;
        }

        private void DefineIndex(Snapshot snapshot)
        {
            // selected expression to be used as index
            BsonExpression selected = null;

            // if index are not defined yet, get index
            if (_query.Index == null)
            {
                // try select best index (or any index)
                var indexScore = this.ChooseIndex(snapshot);

                // if found an index, use-it
                if (indexScore != null)
                {
                    _query.Index = indexScore.CreateIndexQuery();
                }
                else
                {
                    // try create an index
                    indexScore = this.TryCreateIndex(snapshot);

                    if (indexScore != null)
                    {
                        _query.Index = indexScore.CreateIndexQuery();
                    }
                    else
                    {
                        // if no index was created, use full scan over _id
                        _query.Index = new IndexAll("_id", _order);
                    }
                }

                // get selected expression used as index
                selected = indexScore?.Expression;

                // fill index score
                _query.IndexScore = indexScore?.Score ?? 0;
            }

            // fill filter using all expressions
            _query.Filters.AddRange(_where.Where(x => x != selected));
        }

        /// <summary>
        /// Try select best index (highest score) to this list of where expressions
        /// </summary>
        private IndexScore ChooseIndex(Snapshot snapshot)
        {
            var indexes = snapshot.CollectionPage.GetIndexes(true).ToArray();
            IndexScore highest = null;

            // test all possible condition in where (must be conditional)
            foreach (var expr in _where.Where(x => x.IsConditional))
            {
                // get index that match with expression left/right side 
                var index = indexes
                    .Where(x => expr.Left.Source == x.Expression && expr.Left.IsImmutable && expr.Right.IsConstant)
                    .Select(x => Tuple.Create(x, expr.Right))
                    .Union(indexes
                        .Where(x => expr.Right.Source == x.Expression && expr.Right.IsImmutable && expr.Left.IsConstant)
                        .Select(x => Tuple.Create(x, expr.Left))
                    ).FirstOrDefault();

                if (index == null) continue;

                // calculate index score and store highest score
                var current = new IndexScore(index.Item1, expr, index.Item2);

                if (highest == null || current.Score > highest.Score)
                {
                    highest = current;
                }
            }

            return highest;
        }

        /// <summary>
        /// Try create an index over collection using _where conditionals.
        /// </summary>
        private IndexScore TryCreateIndex(Snapshot snapshot)
        {
            // at least a minimum document count
            if (snapshot.CollectionPage.DocumentCount < DOCUMENT_COUNT_TO_CREATE_INDEX) return null;

            // get a valid expression in where
            // must be condition, left side must be a path and immutable and right side must be a constant
            var expr = _where
                .Where(x => x.IsConditional && x.Left.Type == BsonExpressionType.Path && x.Left.IsImmutable && x.Right.IsConstant)
                .OrderBy(x => x.Type)
                .FirstOrDefault();

            // not a good condition? do not create index
            if (expr == null) return null;

            // create random/unique name 
            var name = "idx_auto_" + Guid.NewGuid().ToString("n").Substring(0, 5).ToLower();

            // create index
            _engine.EnsureIndex(_collection, name, expr.Left, false, _transaction);

            var index = snapshot.CollectionPage.GetIndex(name);

            // create index score
            var score = new IndexScore(index, expr, expr.Right);

            return score;
        }
    }
}