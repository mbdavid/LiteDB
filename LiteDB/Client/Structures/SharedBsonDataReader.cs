using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public class SharedBsonDataReader : IBsonDataReader
    {
        private readonly IBsonDataReader _reader;
        private readonly SharedEngine _engine;

        internal SharedBsonDataReader(IBsonDataReader reader, SharedEngine engine)
        {
            _reader = reader;
            _engine = engine;
        }

        public BsonValue this[string field] => _reader[field];

        public string Collection => _reader.Collection;

        public BsonValue Current => _reader.Current;

        public bool HasValues => _reader.HasValues;

        public bool Read() => _reader.Read();

        public void Dispose()
        {
            _reader.Dispose();

            _engine.CloseShared();
        }
    }
}