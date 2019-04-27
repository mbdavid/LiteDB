using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Class that optimize query transforming QueryDefinition into QueryPlan
    /// </summary>
    internal class QueryOptimization
    {
        private readonly Snapshot _snapshot;
        private readonly QueryDefinition _queryDefinition;
        private readonly QueryPlan _query;
        private readonly List<BsonExpression> _terms = new List<BsonExpression>();

        public QueryOptimization(Snapshot snapshot, QueryDefinition queryDefinition, IEnumerable<BsonDocument> source)
        {
            _snapshot = snapshot;
            _queryDefinition = queryDefinition;

            _query = new QueryPlan(snapshot.CollectionName)
            {
                // define index only if source are external collection
                Index = source != null ? new IndexVirtual(source) : null,
                Select = new Select(queryDefinition.Select, queryDefinition.SelectAll),
                ForUpdate = queryDefinition.ForUpdate,
                Limit = queryDefinition.Limit,
                Offset = queryDefinition.Offset
            };
        }

        /// <summary>
        /// Check some rules if query contains concise rules
        /// </summary>
        public void Validate()
        {
            if (_queryDefinition.SelectAll && _queryDefinition.Select == null)
            {
                throw new LiteException(0, "Select ALL require SELECT expression");
            }
            if (_queryDefinition.SelectAll && _queryDefinition.GroupBy != null)
            {
                throw new LiteException(0, "Select ALL has no support for GROUP BY expression");
            }
            if (_queryDefinition.Having != null && _queryDefinition.GroupBy == null)
            {
                throw new LiteException(0, "HAVING require GROUP BY expression");
            }
        }

        /// <summary>
        /// Build QueryPlan instance based on QueryBuilder fields
        /// - Load used fields in all expressions
        /// - Select best index option
        /// - Fill includes 
        /// - Define orderBy
        /// - Define groupBy
        /// </summary>
        public QueryPlan ProcessQuery()
        {
            // split where expressions into TERMs (splited by AND operator)
            this.SplitWherePredicateInTerms();

            // define Fields
            this.DefineQueryFields();

            // define Index, IndexCost, IndexExpression, IsIndexKeyOnly + Where (filters - index)
            this.DefineIndex();

            // define OrderBy
            this.DefineOrderBy();

            // define GroupBy
            this.DefineGroupBy();

            // define IncludeBefore + IncludeAfter
            this.DefineIncludes();

            return _query;
        }

        #region Split Where

        /// <summary>
        /// Fill terms from where predicate list
        /// </summary>
        private void SplitWherePredicateInTerms()
        {
            void add(BsonExpression predicate)
            {
                // add expression in where list breaking AND statments
                if (predicate.IsPredicate || predicate.Type == BsonExpressionType.Or)
                {
                    _terms.Add(predicate);
                }
                else if (predicate.Type == BsonExpressionType.And)
                {
                    var left = predicate.Left;
                    var right = predicate.Right;

                    left.Parameters.Extend(predicate.Parameters);
                    right.Parameters.Extend(predicate.Parameters);

                    add(left);
                    add(right);
                }
                else
                {
                    throw LiteException.InvalidExpressionTypePredicate(predicate);
                }
            }

            // check all where predicate for AND operators
            foreach(var predicate in _queryDefinition.Where)
            {
                add(predicate);
            }
        }

        #endregion

        #region Document Fields

        /// <summary>
        /// Load all fields that must be deserialize from document.
        /// </summary>
        private void DefineQueryFields()
        {
            // load only query fields (null return all document)
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // include all fields detected in all used expressions
            fields.AddRange(_queryDefinition.Select?.Fields ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "$" });
            fields.AddRange(_terms.SelectMany(x => x.Fields));
            fields.AddRange(_queryDefinition.Includes.SelectMany(x => x.Fields));
            fields.AddRange(_queryDefinition.GroupBy?.Fields);
            fields.AddRange(_queryDefinition.Having?.Fields);
            fields.AddRange(_queryDefinition.OrderBy?.Fields);

            // if contains $, all fields must be deserialized
            if (fields.Contains("$"))
            {
                fields.Clear();
            }

            _query.Fields = fields;
        }

        #endregion

        #region Index Definition

        private void DefineIndex()
        {
            // selected expression to be used as index (from _terms)
            BsonExpression selected = null;

            // if index are not defined yet, get index
            if (_query.Index == null)
            {
                // try select best index (if return null, there is no good choice)
                var indexCost = this.ChooseIndex(_query.Fields);

                // if found an index, use-it
                if (indexCost != null)
                {
                    _query.Index = indexCost.Index;
                    _query.IndexCost = indexCost.Cost;
                    _query.IndexExpression = indexCost.IndexExpression;
                }
                else
                {
                    // if no index found, use FULL COLLECTION SCAN (has no data order)
                    var data = new DataService(_snapshot);

                    _query.Index = new IndexVirtual(data.ReadAll(_query.Fields));
                }

                // get selected expression used as index
                selected = indexCost?.Expression;
            }
            else
            {
                ENSURE(_query.Index is IndexVirtual, "pre-defined index must be only for virtual collections");

                _query.IndexCost = 0;
            }

            // if is only 1 field to deserialize and this field are same as index, use IndexKeyOnly = rue
            if (_query.Fields.Count == 1 && _query.IndexExpression == "$." + _query.Fields.First())
            {
                // best choice - no need lookup for document (use only index)
                _query.IsIndexKeyOnly = true;
            }

            // fill filter using all expressions (remove selected term used in Index)
            _query.Filters.AddRange(_terms.Where(x => x != selected));
        }

        /// <summary>
        /// Try select index based on lowest cost or GroupBy/OrderBy reuse - use this priority order:
        /// - Get lowest index cost used in WHERE expressions (will filter data)
        /// - If there is no candidate, try get:
        ///     - Same of GroupBy
        ///     - Same of OrderBy
        ///     - Prefered single-field (when no lookup neeed)
        /// </summary>
        private IndexCost ChooseIndex(HashSet<string> fields)
        {
            var indexes = _snapshot.CollectionPage.GetCollectionIndexes().ToArray();

            // if query contains a single field used, give preferred if this index exists
            var preferred = fields.Count == 1 ? "$." + fields.First() : null;

            // otherwise, check for lowest index cost
            IndexCost lowest = null;

            // test all possible predicates in terms
            foreach (var expr in _terms.Where(x => x.IsPredicate))
            {
                ENSURE(expr.Left != null && expr.Right != null, "predicate expression must has left/right expressions");

                // get index that match with expression left/right side 
                var index = indexes
                    .Where(x => x.Expression == expr.Left.Source && expr.Right.IsValue)
                    .Select(x => Tuple.Create(x, expr.Right))
                    .Union(indexes
                        .Where(x => x.Expression == expr.Right.Source && expr.Left.IsValue)
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
            if (lowest == null && (_queryDefinition.OrderBy != null || _queryDefinition.GroupBy != null || preferred != null))
            {
                var index =
                    indexes.FirstOrDefault(x => x.Expression == _queryDefinition.GroupBy?.Source) ??
                    indexes.FirstOrDefault(x => x.Expression == _queryDefinition.OrderBy?.Source) ??
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
        private void DefineOrderBy()
        {
            // if has no order by, returns null
            if (_queryDefinition.OrderBy == null) return;

            var orderBy = new OrderBy(_queryDefinition.OrderBy, _queryDefinition.Order);

            // if index expression are same as orderBy, use index to sort - just update index order
            if (orderBy.Expression.Source == _query.IndexExpression)
            {
                // TODO: analyze SELECT expression to avoid wrong re-use of index
                // here, optimization need detect if there any SELECT that transform data
                // remember: order by runs AFTER select, so same field in SELECT transform could be not same
                // as in original collection
                // eg: SELECT { _id: name } FROM zip ORDER BY _id;
                // in this example, "OrderBy _id" need order "by name" because _id was override

                // re-use index order and no not run OrderBy
                // update index order to be same as required in OrderBy
                _query.Index.Order = orderBy.Order;

                // in this case "query.OrderBy" will be null
                orderBy = null;
            }

            // otherwise, query.OrderBy will be setted according user defined
            _query.OrderBy = orderBy;
        }

        /// <summary>
        /// Define GroupBy optimization (try re-use index)
        /// </summary>
        private void DefineGroupBy()
        {
            if (_queryDefinition.GroupBy == null) return;

            var groupBy = new GroupBy(_queryDefinition.GroupBy, _queryDefinition.Select, _queryDefinition.Having);

            // if groupBy use same expression in index, set group by order to MaxValue to not run
            if (groupBy.Expression.Source == _query.IndexExpression)
            {
                // here there is no problem as in OrderBy because GroupBy order occurs BEFORE select transform

                // do not sort when run groupBy (already sorted by index)
                groupBy.Order = 0; // 0 means "none"
            }
            else
            {
                // by default, groupBy sort as ASC only
                groupBy.Order = Query.Ascending;
            }

            _query.GroupBy = groupBy;
        }

        #endregion

        /// <summary>
        /// Will define each include to be run BEFORE where (worst) OR AFTER where (best)
        /// </summary>
        private void DefineIncludes()
        {
            foreach(var include in _queryDefinition.Includes)
            {
                // includes always has one single field
                var field = include.Fields.Single();

                // test if field are using in any filter
                var used = _query.Filters.Any(x => x.Fields.Contains(field));

                if (used)
                {
                    _query.IncludeBefore.Add(include);
                }
                else
                {
                    _query.IncludeAfter.Add(include);
                }
            }
        }
    }
}