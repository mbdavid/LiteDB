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
        /// Build QueryPlan instance based on QueryBuilder fields
        /// - Load used fields in all expressions
        /// - Select best index option
        /// - Fill includes 
        /// - Define orderBy
        /// - Define groupBy
        /// </summary>
        private QueryPlan OptimizeQuery(Snapshot snapshot)
        {
            var query = new QueryPlan(_collection)
            {
                Index = _index,
                Select = _select,
                ForUpdate = _forUpdate,
                Limit = _limit,
                Offset = _offset
            };

            // define Fields
            this.DefineQueryFields(query);

            // define Index, IndexCost, IndexExpression, IsIndexKeyOnly + Where (filters - index)
            this.DefineIndex(query, snapshot);

            // define OrderBy
            this.DefineOrderBy(query);

            // define GroupBy
            this.DefineGroupBy(query);

            // define IncludeBefore + IncludeAfter
            this.DefineIncludes(query);

            return query;
        }

        #region Document Fields

        /// <summary>
        /// Load all fields that must be deserialize from document.
        /// </summary>
        private void DefineQueryFields(QueryPlan query)
        {
            // load only query fields (null return all document)
            var fields = new HashSet<string>();

            // include all fields detected in all used expressions
            fields.AddRange(_select?.Expression.Fields ?? new HashSet<string> { "$" });
            fields.AddRange(_where.SelectMany(x => x.Fields));
            fields.AddRange(_includes.SelectMany(x => x.Fields));
            fields.AddRange(_groupBy?.Expression.Fields);
            fields.AddRange(_groupBy?.Select?.Fields);
            fields.AddRange(_groupBy?.Having?.Fields);
            fields.AddRange(_orderBy?.Expression.Fields);

            // if contains $, all fields must be deserialized
            if (fields.Contains("$"))
            {
                fields.Clear();
            }

            query.Fields = fields;
        }

        #endregion

        #region Index Definition

        private void DefineIndex(QueryPlan query, Snapshot snapshot)
        {
            // selected expression to be used as index
            BsonExpression selected = null;

            // if index are not defined yet, get index
            if (query.Index == null)
            {
                // try select best index (or any index)
                var indexCost = this.ChooseIndex(snapshot, query.Fields);

                // if found an index, use-it
                if (indexCost != null)
                {
                    query.Index = indexCost.Index;
                    query.IndexCost = indexCost.Cost;
                    query.IndexExpression = indexCost.IndexExpression;
                }
                else
                {
                    // if has no index to use, use full scan over _id
                    var pk = snapshot.CollectionPage.GetIndex(0);

                    query.Index = new IndexAll("_id", Query.Ascending);
                    query.IndexCost = query.Index.GetCost(pk);
                    query.IndexExpression = "$._id";
                }

                // get selected expression used as index
                selected = indexCost?.Expression;
            }
            else
            {
                // find query user defined index (must exists)
                var idx = snapshot.CollectionPage.GetIndex(query.Index.Name);

                if (idx == null) throw LiteException.IndexNotFound(query.Index.Name, snapshot.CollectionPage.CollectionName);

                query.IndexCost = query.Index.GetCost(idx);
                query.IndexExpression = idx.Expression;
            }

            // if is only 1 field to deserialize and this field are same as index, use IndexKeyOnly = rue
            if (query.Fields.Count == 1 && query.IndexExpression == "$." + query.Fields.First())
            {
                query.IsIndexKeyOnly = true;
            }

            // fill filter using all expressions
            query.Filters.AddRange(_where.Where(x => x != selected));
        }

        /// <summary>
        /// Try select best index (lowest cost) to this list of where expressions
        /// </summary>
        private IndexCost ChooseIndex(Snapshot snapshot, HashSet<string> fields)
        {
            var indexes = snapshot.CollectionPage.GetIndexes(true).ToArray();

            // if query contains a single field used, give preferred if this index exists
            var preferred = fields.Count == 1 ? "$." + fields.First() : null;

            // otherwise, check for lowest index cost
            IndexCost lowest = null;

            // test all possible predicates in where (exclude OR/ANR)
            foreach (var expr in _where.Where(x => x.IsPredicate))
            {
                // get index that match with expression left/right side 
                var index = indexes
                    .Where(x => x.Expression == expr.Left.Source && expr.Right.IsConstant)
                    .Select(x => Tuple.Create(x, expr.Right))
                    .Union(indexes
                        .Where(x => x.Expression == expr.Right.Source && expr.Left.IsConstant)
                        .Select(x => Tuple.Create(x, expr.Left))
                    ).FirstOrDefault();

                if (index == null) continue;

                // calculate index score and store highest score
                var current = new IndexCost(index.Item1, expr, index.Item2);

                if (lowest == null || current.Cost < lowest.Cost)
                {
                    lowest = current;
                }
            }

            // if no index found, try use same index in orderby/groupby/preferred
            if (lowest == null && (_orderBy != null || _groupBy != null || preferred != null))
            {
                var index = 
                    indexes.FirstOrDefault(x => x.Expression == _orderBy?.Expression.Source) ??
                    indexes.FirstOrDefault(x => x.Expression == _groupBy?.Expression.Source) ??
                    indexes.FirstOrDefault(x => x.Expression == preferred);

                if (index != null)
                {
                    lowest = new IndexCost(index);
                }
            }

            return lowest;
        }

        #endregion

        #region OrderBy / GroupBy Definition

        /// <summary>
        /// Define OrderBy optimization (try re-use index)
        /// </summary>
        private void DefineOrderBy(QueryPlan query)
        {
            // if has no order by, returns null
            if (_orderBy == null) return;

            // if index expression are same as orderBy, use index to sort - just update index order
            if (_orderBy.Expression.Source == query.IndexExpression)
            {
                // re-use index order and no not run OrderBy
                query.Index.Order = _orderBy.Order;
            }
            else
            {
                query.OrderBy = _orderBy;
            }
        }

        /// <summary>
        /// Define GroupBy optimization (try re-use index)
        /// </summary>
        private void DefineGroupBy(QueryPlan query)
        {
            if (_groupBy == null) return;

            // if groupBy use same expression in index, set group by order to MaxValue to not run
            if (_groupBy.Expression.Source == query.IndexExpression)
            {
                // update index order tu use same as group by (only if has no order by defined)
                if (_groupBy.Order == query.Index.Order || _orderBy == null)
                {
                    _groupBy.Order = 0;
                    query.Index.Order = _groupBy.Order;
                }
            }

            query.GroupBy = _groupBy;
        }

        #endregion

        /// <summary>
        /// Will define each include to be run BEFORE where (worst) OR AFTER where (best)
        /// </summary>
        private void DefineIncludes(QueryPlan query)
        {
            foreach(var include in _includes)
            {
                // includes always has one single field
                var field = include.Fields.Single();

                // test if field are using in any filter
                var used = query.Filters.Any(x => x.Fields.Contains(field));

                if (used)
                {
                    query.IncludeBefore.Add(include);
                }
                else
                {
                    query.IncludeAfter.Add(include);
                }
            }
        }
    }
}