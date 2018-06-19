using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB.Engine
{
    /// <summary>
    /// Class to provider a fluent query API to complex queries. This class will be optimied to convert into Query class before run
    /// </summary>
    public partial class QueryBuilder
    {
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

            // try merge multiples OR into same IN conditional
            this.TryMergeOrExpression();

            // define index (can create if needed)
            this.DefineIndex(snapshot);

            // try re-use same index order or define a new one
            this.DefineOrderByGroupBy(snapshot);

            // load all fields to be deserialize in document
            this.LoadQueryFields();

            _optimized = true;
        }

        #region Merge OR clausule

        /// <summary>
        /// Find OR expression with same left side + operator to use IN operator
        /// </summary>
        private void TryMergeOrExpression()
        {
            //TODO implement TryMergeOrExpression optimization
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
                    _query.IndexExpression = indexCost.Expression.Source;
                }
                else
                {
                    // if has no index to use, use full scan over _id
                    var pk = snapshot.CollectionPage.GetIndex(0);

                    _query.Index = new IndexAll("_id", _order);
                    _query.IndexCost = _query.Index.GetCost(pk);
                    _query.IndexExpression = "$._id";
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
                _query.IndexExpression = idx.Expression;
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

            // if no index found, try use same index in orderby/groupby
            if (lowest == null && (_orderBy != null || _query.GroupBy != null))
            {
                var index = 
                    indexes.FirstOrDefault(x => x.Expression == _orderBy?.Source) ??
                    indexes.FirstOrDefault(x => x.Expression == _query.GroupBy?.Source);

                if (index != null)
                {
                    lowest = new IndexCost(index);
                }
            }

            return lowest;
        }

        #endregion

        #region OrderBy Definition

        /// <summary>
        /// Define order expression and try re-use same index order by (if possible)
        /// </summary>
        private void DefineOrderByGroupBy(Snapshot snapshot)
        {
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
            if (_orderBy?.Source == index.Expression)
            {
                _query.Index.Order = _order;
            }
            else
            {
                _query.OrderBy = _orderBy;
                _query.Order = _order;
            }

            // if groupby use same expression in index, set group by order to MaxValue to not run
            if (_query.GroupBy?.Source == index.Expression)
            {
                _query.RunOrderByOverGroupBy = false;
                _query.Index.Order = _query.GroupByOrder;
            }
        }

        #endregion

        /// <summary>
        /// Load all fields that must be deserialize from document. If is possible use only key (without no document deserialization) set KeyOnly = true
        /// </summary>
        private void LoadQueryFields()
        {
            // if select was not defined, define as full document read
            if (_query.Select == null)
            {
                _query.Select = BsonExpression.Create("$");
            }

            // load only query fields (null return all document)
            _query.Fields = _query.Select.Fields;

            // if partial document load, add filter, groupby, orderby fields too
            _query.Fields.AddRange(_query.Filters.SelectMany(x => x.Fields));
            _query.Fields.AddRange(_query.GroupBy?.Fields);
            _query.Fields.AddRange(_query.OrderBy?.Fields);

            if (_query.Fields.Contains("$"))
            {
                _query.Fields = new HashSet<string> { "$" };
            }
            else if(_query.Fields.Count == 1 && _query.IndexExpression == "$." + _query.Fields.First())
            {
                // if need only 1 key and is same as used in index, do not deserialize document
                _query.KeyOnly = true;
            }
        }
    }
}