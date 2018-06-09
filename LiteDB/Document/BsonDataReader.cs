using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class to read void, one or a collection of BsonValues. Used in SQL execution commands
    /// </summary>
    public class BsonDataReader : IEnumerable<BsonValue>, IDisposable
    {
        private BsonValue _single;
        private IEnumerator<BsonValue> _source;

        /// <summary>
        /// Initialize with no value
        /// </summary>
        public BsonDataReader()
        {
        }

        /// <summary>
        /// Initialize with a single value
        /// </summary>
        public BsonDataReader(BsonValue value)
        {
            _single = value;
        }

        /// <summary>
        /// Initialize with an IEnumerable data source
        /// </summary>
        public BsonDataReader(IEnumerable<BsonValue> values)
        {
            _source = values.GetEnumerator();
        }

        /// <summary>
        /// Return current value
        /// </summary>
        public BsonValue Value => _single ?? _source.Current;

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

        public IEnumerator<BsonValue> GetEnumerator()
        {
            return _source ?? (_single != null ? 
                new List<BsonValue> { _single }.GetEnumerator() :
                new List<BsonValue>().GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _source ?? (_single != null ?
                new List<BsonValue> { _single }.GetEnumerator() :
                new List<BsonValue>().GetEnumerator());
        }

        public void Dispose()
        {
            _source?.Dispose();
        }
    }
}