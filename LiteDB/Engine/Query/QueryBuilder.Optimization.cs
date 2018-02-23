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
            // if this query already optimazed, do not optmize again
            if (_optimized) return;

            // try merge multiples OR into same conditional
            this.TryMergeOrExpression();

            // define index (can create if needed)
            this.DefineIndex(snapshot);

            // try re-use same index order or define a new one
            this.DefineOrderBy(snapshot);

            _optimized = true;
        }

        #region Merge OR clausule

        /// <summary>
        /// Find OR expression with same left side + operator
        /// </summary>
        private void TryMergeOrExpression()
        {


        }


        #endregion

        #region Index Definition

        private void DefineIndex(Snapshot snapshot)
        {
            // selected expression to be used as index
            BsonExpression selected = null;

            // if index are not defined yet, get index
            if (_query.Index == null)
            {
                // try select best index (or any index)
                var indexCost = this.ChooseIndex(snapshot);

                // if found an index, use-it
                if (indexCost != null)
                {
                    _query.Index = indexCost.Index;
                    _query.IndexCost = indexCost.Cost;
                }
                else
                {
                    // try create an index
                    indexCost = this.TryCreateIndex(snapshot);

                    if (indexCost != null)
                    {
                        _query.Index = indexCost.Index;
                        _query.IndexCost = indexCost.Cost;
                    }
                    else
                    {
                        // if no index was created, use full scan over _id
                        var pk = snapshot.CollectionPage.GetIndex(0);

                        _query.Index = new IndexAll("_id", _order);
                        _query.IndexCost = _query.Index.GetCost(pk);
                    }
                }

                // get selected expression used as index
                selected = indexCost?.Expression;
            }
            else
            {
                // find query user defined index (must exists)
                var idx = snapshot.CollectionPage.GetIndex(_query.Index.Name);

                if (idx == null) throw LiteException.IndexNotFound(_query.Index.Name, snapshot.CollectionPage.CollectionName);

                _query.IndexCost = _query.Index.GetCost(idx);
            }

            // fill filter using all expressions
            _query.Filters.AddRange(_where.Where(x => x != selected));
        }

        /// <summary>
        /// Try select best index (lowest cost) to this list of where expressions
        /// </summary>
        private IndexCost ChooseIndex(Snapshot snapshot)
        {
            var indexes = snapshot.CollectionPage.GetIndexes(true).ToArray();

            IndexCost lowest = null;

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
                var current = new IndexCost(index.Item1, expr, index.Item2);

                if (lowest == null || lowest.Cost < current.Cost)
                {
                    lowest = current;
                }
            }

            return lowest;
        }

        /// <summary>
        /// Try create an index over collection using _where conditionals.
        /// </summary>
        private IndexCost TryCreateIndex(Snapshot snapshot)
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

            // get index cost
            var cost = new IndexCost(index, expr, expr.Right);

            return cost;
        }

        #endregion

        #region OrderBy Definition

        /// <summary>
        /// Define order expression and try re-use same index order by (if possible)
        /// </summary>
        private void DefineOrderBy(Snapshot snapshot)
        {
            if (_orderBy == null) return;

            // if index use OR, is not valid for orderBy
            if (_query.Index is IndexOr)
            {
                _query.OrderBy = _orderBy;
                _query.Order = _order;
                return;
            }

            // get index (here, always exists - never return null)
            var index = snapshot.CollectionPage.GetIndex(_query.Index.Name);

            // if index expression are same as orderBy, use index to sort - just update index order
            if (index.Expression == _orderBy.Source)
            {
                _query.Index.Order = _order;
            }
            else
            {
                _query.OrderBy = _orderBy;
                _query.Order = _order;
            }
        }

        #endregion

    }
}