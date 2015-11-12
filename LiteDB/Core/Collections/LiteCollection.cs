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
        private DbEngine _engine;
        private string _name;
        private BsonMapper _mapper;
        private List<Action<BsonDocument>> _includes;
        private QueryVisitor<T> _visitor;

        /// <summary>
        /// Get collection name
        /// </summary>
        public string Name { get { return _name; } }

        internal LiteCollection(string name, DbEngine engine, BsonMapper mapper)
        {
            _name = name;
            _engine = engine;
            _mapper = mapper;
            _visitor = new QueryVisitor<T>(mapper);
            _includes = new List<Action<BsonDocument>>();
        }
    }
}
