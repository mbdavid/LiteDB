using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class to read void, one or a collection of BsonValues. Used in SQL execution commands
    /// </summary>
    public class BsonDataReader : IDisposable
    {
        private BsonValue _current = null;
        private IEnumerator<BsonValue> _source = null;
        private string _collection = null;
        private bool _isFirst;
        private bool _hasValues;

        /// <summary>
        /// Handler function when NextResult() called - return null if no more data
        /// </summary>
        public event Func<BsonDataReader> FetchNextResult = null;

        /// <summary>
        /// Initialize with no value
        /// </summary>
        internal BsonDataReader()
        {
            _hasValues = false;
        }

        /// <summary>
        /// Initialize with a single value
        /// </summary>
        internal BsonDataReader(BsonValue value, string collection = null)
        {
            _current = value;
            _isFirst = _hasValues = true;
            _collection = collection;
        }

        /// <summary>
        /// Initialize with an IEnumerable data source
        /// </summary>
        internal BsonDataReader(IEnumerable<BsonValue> values, string collection)
        {
            _source = values.GetEnumerator();
            _collection = collection;

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
            var next = this.FetchNextResult?.Invoke();

            if (next == null) return false;

            _current = next._current;
            _source = next._source;
            _collection = next._collection;
            _isFirst = next._isFirst;
            _hasValues = next._hasValues;

            return true;
        }

        /// <summary>
        /// Return true if data reader contains multiple values (recordset)
        /// </summary>
        public bool IsRecordset => _source != null;

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
        public string Collection => _collection;

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