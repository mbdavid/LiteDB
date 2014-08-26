using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Class helper to create query using indexes in database. All methods are statics
    /// </summary>
    public class Query
    {
        private string Field;
        private string Operador;
        private object Value;
        private object ValueEnd;
        private StringComparison ComparisonType;

        private Query()
        {
        }

        /// <summary>
        /// Returns all objects
        /// </summary>
        public static Query All()
        {
            return new Query { Field = "_id", Operador = "all" };
        }

        /// <summary>
        /// Returns all objects that value are equals to value (=)
        /// </summary>
        public static Query EQ(string field, object value)
        {
            return new Query { Field = field, Operador = "=", Value = value };
        }

        /// <summary>
        /// Returns all objects that value are less than value (<)
        /// </summary>
        public static Query LT(string field, object value)
        {
            return new Query { Field = field, Operador = "<", Value = value };
        }

        /// <summary>
        /// Returns all objects that value are less than or equals value (<=)
        /// </summary>
        public static Query LTE(string field, object value)
        {
            return new Query { Field = field, Operador = "<=", Value = value };
        }

        /// <summary>
        /// Returns all objects that value are greater than value (>)
        /// </summary>
        public static Query GT(string field, object value)
        {
            return new Query { Field = field, Operador = ">", Value = value };
        }

        /// <summary>
        /// Returns all objects that value are greater than or equals value (>=)
        /// </summary>
        public static Query GTE(string field, object value)
        {
            return new Query { Field = field, Operador = ">=", Value = value };
        }

        /// <summary>
        /// Returns all objects that values are between "start" and "end" values (BETWEEN)
        /// </summary>
        public static Query Between(string field, object start, object end)
        {
            return new Query { Field = field, Operador = "between", Value = start, ValueEnd = end };
        }

        /// <summary>
        /// Returns all objects that starts with value (LIKE)
        /// </summary>
        public static Query StartsWith(string field, object value, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
        {
            return new Query { Field = field, Operador = "startswith", Value = value, ComparisonType = comparisonType };
        }

        /// <summary>
        /// Returns all objects that has value in values list (IN)
        /// </summary>
        public static Query In(string field, params object[] values)
        {
            return new Query { Field = field, Operador = "in", Value = values };
        }

        /// <summary>
        /// Returns all objects that are not equals to value
        /// </summary>
        public static Query Not(string field, object value)
        {
            return new Query { Field = field, Operador = "not", Value = value };
        }

        /// <summary>
        /// Returns objects that exists in ALL queries results.
        /// </summary>
        public static Query AND(params Query[] queries)
        {
            return new Query { Field = "_id", Operador = "and", Value = queries };
        }

        /// <summary>
        /// Returns objects that exists in ANY queries results.
        /// </summary>
        public static Query OR(params Query[] queries)
        {
            return new Query { Field = "_id", Operador = "or", Value = queries };
        }

        internal IEnumerable<IndexNode> Execute(LiteEngine engine, CollectionPage col)
        {
            var index = col.Indexes.FirstOrDefault(x => x.Field.Equals(this.Field, StringComparison.InvariantCultureIgnoreCase));

            // auto-create a index on this field
            if (index == null)
            {
                engine.GetCollection(col.CollectionName).EnsureIndex(this.Field);
            }

            if (index == null) throw new LiteDBException(string.Format("Index '{0}.{1}' not found. Use EnsureIndex to create a new index.", col.CollectionName, this.Field));

            switch (this.Operador)
            {
                // indexed operations
                case "=": return engine.Indexer.FindEquals(index, this.Value);
                case "<": return engine.Indexer.FindLessThan(index, this.Value, false);
                case "<=": return engine.Indexer.FindLessThan(index, this.Value, true);
                case ">": return engine.Indexer.FindGreaterThan(index, this.Value, false);
                case ">=": return engine.Indexer.FindGreaterThan(index, this.Value, true);
                case "between": return engine.Indexer.FindBetween(index, this.Value, this.ValueEnd);
                case "startswith": return engine.Indexer.FindStarstWith(index, this.Value.ToString(), this.ComparisonType);
                case "in": return engine.Indexer.FindIn(index, (object[])this.Value);
                // index full scan (not Document full scan)
                case "not": return engine.Indexer.FindAll(index).Where(x => x.Key.CompareTo(new IndexKey(this.Value)) != 0);
                case "all": return engine.Indexer.FindAll(index);
                // AND/OR operations
                case "and":
                    {
                        var queries = (Query[])this.Value;
                        var results = queries[0].Execute(engine, col);

                        for (var i = 1; i < queries.Length; i++)
                        {
                            var q = queries[i];
                            results = results.Intersect(q.Execute(engine, col));
                        }
                        return results;
                    }
                case "or":
                    {
                        var queries = (Query[])this.Value;
                        var results = queries[0].Execute(engine, col);

                        for (var i = 1; i < queries.Length; i++)
                        {
                            var q = queries[i];
                            results = results.Union(q.Execute(engine, col));
                        }
                        return results;
                    }
            }

            throw new NotImplementedException();
        }

    }
}
