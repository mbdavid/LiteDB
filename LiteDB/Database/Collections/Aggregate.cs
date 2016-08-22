using System;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        #region Count

        /// <summary>
        /// Get document count using property on collection.
        /// </summary>
        public int Count()
        {
            // do not use indexes - collections has DocumentCount property
            return (int)_engine.Count(_name, null);
        }

        /// <summary>
        /// Count documnets with a query. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public int Count(Query query)
        {
            if (query == null) throw new ArgumentNullException("query");

            // keep trying execute query to auto-create indexes when not found
            while (true)
            {
                try
                {
                    return (int)_engine.Count(_name, query);
                }
                catch (IndexNotFoundException ex)
                {
                    // if query returns this exception, let's auto create using mapper (or using default options)
                    var options = _mapper.GetIndexFromMapper<T>(ex.Field) ?? new IndexOptions();

                    _engine.EnsureIndex(ex.Collection, ex.Field, options);
                }
            }
        }

        /// <summary>
        /// Count documnets with a query. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public int Count(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("predicate");

            return this.Count(_visitor.Visit(predicate));
        }

        #endregion Count

        #region LongCount

        /// <summary>
        /// Get document count using property on collection.
        /// </summary>
        public long LongCount()
        {
            // do not use indexes - collections has DocumentCount property
            return _engine.Count(_name, null);
        }

        /// <summary>
        /// Count documnets with a query. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public long LongCount(Query query)
        {
            if (query == null) throw new ArgumentNullException("query");

            // keep trying execute query to auto-create indexes when not found
            while (true)
            {
                try
                {
                    return _engine.Count(_name, query);
                }
                catch (IndexNotFoundException ex)
                {
                    // if query returns this exception, let's auto create using mapper (or using default options)
                    var options = _mapper.GetIndexFromMapper<T>(ex.Field) ?? new IndexOptions();

                    _engine.EnsureIndex(ex.Collection, ex.Field, options);
                }
            }
        }

        /// <summary>
        /// Count documnets with a query. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public long LongCount(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("predicate");

            return this.LongCount(_visitor.Visit(predicate));
        }

        #endregion LongCount

        #region Exists

        /// <summary>
        /// Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public bool Exists(Query query)
        {
            if (query == null) throw new ArgumentNullException("query");

            // keep trying execute query to auto-create indexes when not found
            while (true)
            {
                try
                {
                    return _engine.Exists(_name, query);
                }
                catch (IndexNotFoundException ex)
                {
                    // if query returns this exception, let's auto create using mapper (or using default options)
                    var options = _mapper.GetIndexFromMapper<T>(ex.Field) ?? new IndexOptions();

                    _engine.EnsureIndex(ex.Collection, ex.Field, options);
                }
            }
        }

        /// <summary>
        /// Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public bool Exists(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("predicate");

            return this.Exists(_visitor.Visit(predicate));
        }

        #endregion Exits

        #region Min/Max

        /// <summary>
        /// Returns the first/min value from a index field
        /// </summary>
        public BsonValue Min(string field)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            // keep trying execute query to auto-create indexes when not found
            while (true)
            {
                try
                {
                    return _engine.Min(_name, field);
                }
                catch (IndexNotFoundException ex)
                {
                    // if query returns this exception, let's auto create using mapper (or using default options)
                    var options = _mapper.GetIndexFromMapper<T>(ex.Field) ?? new IndexOptions();

                    _engine.EnsureIndex(ex.Collection, ex.Field, options);
                }
            }
        }

        /// <summary>
        /// Returns the first/min _id field
        /// </summary>
        public BsonValue Min()
        {
            return this.Min("_id");
        }

        /// <summary>
        /// Returns the first/min field using a linq expression
        /// </summary>
        public BsonValue Min<K>(Expression<Func<T, K>> property)
        {
            if (property == null) throw new ArgumentNullException("property");

            var field = _visitor.GetBsonField(property);

            return this.Min(field);
        }

        /// <summary>
        /// Returns the last/max value from a index field
        /// </summary>
        public BsonValue Max(string field)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            // keep trying execute query to auto-create indexes when not found
            while (true)
            {
                try
                {
                    return _engine.Max(_name, field);
                }
                catch (IndexNotFoundException ex)
                {
                    // if query returns this exception, let's auto create using mapper (or using default options)
                    var options = _mapper.GetIndexFromMapper<T>(ex.Field) ?? new IndexOptions();

                    _engine.EnsureIndex(ex.Collection, ex.Field, options);
                }
            }
        }

        /// <summary>
        /// Returns the last/max _id field
        /// </summary>
        public BsonValue Max()
        {
            return this.Max("_id");
        }

        /// <summary>
        /// Returns the last/max field using a linq expression
        /// </summary>
        public BsonValue Max<K>(Expression<Func<T, K>> property)
        {
            if (property == null) throw new ArgumentNullException("property");

            var field = _visitor.GetBsonField(property);

            return this.Max(field);
        }

        #endregion Min/Max
    }
}