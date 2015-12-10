using System;
using System.Collections.Generic;

namespace LiteDB
{
    public sealed partial class LiteCollection<T>
        where T : new()
    {
        private string _name;
        private DbEngine _engine;
        private BsonMapper _mapper;
        private Logger _log;
        private List<Action<BsonDocument>> _includes;
        private QueryVisitor<T> _visitor;

        /// <summary>
        /// Get collection name
        /// </summary>
        public string Name { get { return _name; } }

        internal LiteCollection(string name, DbEngine engine, BsonMapper mapper, Logger log)
        {
            _name = name;
            _engine = engine;
            _mapper = mapper;
            _log = log;
            _visitor = new QueryVisitor<T>(mapper);
            _includes = new List<Action<BsonDocument>>();
        }
    }
}