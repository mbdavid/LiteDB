using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public sealed partial class LiteCollection<T>
        where T : new()
    {
        /// <summary>
        /// Default buffer size for insert/update/delete loop operations - transaction will commit in every N docs
        /// </summary>
        private const int BUFFER_SIZE = 1024;

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
