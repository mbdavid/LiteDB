using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public enum BsonDataResultMode { Void, Single, Resultset }

    /// <summary>
    /// Class to read void, one or a collection of BsonValues. Used in SQL execution commands
    /// </summary>
    public class BsonDataReader : IDisposable
    {
        private BsonValue _current = null;
        private IEnumerator<BsonValue> _source = null;
        private QueryPlan _query = null;
        private bool _isFirst;
        private bool _hasValues;
        private readonly BsonDataResultMode _mode;

        /// <summary>
        /// Return type of data reader: Void, Single or Recordset
        /// </summary>
        public BsonDataResultMode Mode => _mode;

        internal Func<BsonDataReader> NextResultFunc = () => null;

        /// <summary>
        /// Initialize with no value
        /// </summary>
        internal BsonDataReader()
        {
            _mode = BsonDataResultMode.Void;
            _hasValues = false;
        }

        /// <summary>
        /// Initialize with a single value
        /// </summary>
        internal BsonDataReader(BsonValue value)
        {
            _mode = BsonDataResultMode.Single;
            _current = value;
            _isFirst = _hasValues = true;
        }

        /// <summary>
        /// Initialize with an IEnumerable data source
        /// </summary>
        internal BsonDataReader(IEnumerable<BsonValue> values, QueryPlan query)
        {
            _mode = BsonDataResultMode.Resultset;
            _source = values.GetEnumerator();
            _query = query;

            if (_source.MoveNext())
            {
                _hasValues = _isFirst = true;
                _current = _source.Current;
            }
        }

        /// <summary>
        /// Advances the data reader to the next result
        /// </summary>
        public bool NextResult()
        {
            // execute func to request next data reader
            var next = this.NextResultFunc();

            if (next == null) return false;

            _current = next._current;
            _source = next._source;
            _query = next._query;
            _isFirst = next._isFirst;
            _hasValues = next._hasValues;

            return true;
        }

        /// <summary>
        /// Return if has any value in result
        /// </summary>
        public bool HasValues => _hasValues;

        /// <summary>
        /// Return current value
        /// </summary>
        public BsonValue Current => _current;

        /// <summary>
        /// Return collection name
        /// </summary>
        public string Collection => _query?.Collection;

        /// <summary>
        /// Get query explain plan
        /// </summary>
        public string ExplainPlan => _query?.GetExplainPlan();

        /// <summary>
        /// Move cursor to next result. Returns true if read was possible
        /// </summary>
        public bool Read()
        {
            if (!_hasValues) return false;

            if (_isFirst)
            {
                _isFirst = false;
                return true;
            }
            else
            {
                if (_source != null)
                {
                    var read = _source.MoveNext();
                    _current = _source.Current;
                    return read;
                }
                else
                {
                    return false;
                }
            }
        }
        
        public BsonValue this[string field]
        {
            get
            {
                return _current.AsDocument[field] ?? BsonValue.Null;
            }
        }

        public void Dispose()
        {
            _source?.Dispose();
        }
    }
}