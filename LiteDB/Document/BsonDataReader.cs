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
        private BsonValue _single;
        private IEnumerator<BsonValue> _source;
        private string _collection = null;

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
        }

        /// <summary>
        /// Return current value
        /// </summary>
        public BsonValue Value => _single ?? _source.Current;

        /// <summary>
        /// Return collection name
        /// </summary>
        public string Collection => _collection;

        /// <summary>
        /// Move cursor to next result. Returns true if read was possible
        /// </summary>
        public bool Read()
        {
            return _source?.MoveNext() ?? false;
        }
        
        public BsonValue this[string field]
        {
            get
            {
                return _single?.AsDocument[field] ?? _source?.Current.AsDocument[field] ?? BsonValue.Null;
            }
        }

        public void Dispose()
        {
            _source?.Dispose();
        }
    }
}