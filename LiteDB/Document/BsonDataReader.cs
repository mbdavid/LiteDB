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
        private BsonValue _single = null;
        private IEnumerator<BsonValue> _source = null;
        private string _collection = null;
        private bool _hasValues = false;
        private BsonValue _first = null;
        private bool _isFirst = false;

        /// <summary>
        /// Initialize with no value
        /// </summary>
        internal BsonDataReader()
        {
        }

        /// <summary>
        /// Initialize with a single value
        /// </summary>
        internal BsonDataReader(BsonValue value)
        {
            _single = value;
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
                _first = _source.Current;
            }
        }

        /// <summary>
        /// Return if has any value in result
        /// </summary>
        public bool HasValues => (_single != null) || _hasValues;

        /// <summary>
        /// Return current value
        /// </summary>
        public BsonValue Current => _single ?? _first ?? _source.Current;

        /// <summary>
        /// Return collection name
        /// </summary>
        public string Collection => _collection;

        /// <summary>
        /// Move cursor to next result. Returns true if read was possible
        /// </summary>
        public bool Read()
        {
            if (_single != null) return false;
            if (_source == null) return false;

            if (_isFirst)
            {
                _isFirst = false;
                return true;
            }
            else
            {
                _first = null;
                return _source.MoveNext();
            }
        }
        
        public BsonValue this[string field]
        {
            get
            {
                return this.Current?.AsDocument[field] ?? BsonValue.Null;
            }
        }

        public void Dispose()
        {
            _source?.Dispose();
        }
    }
}