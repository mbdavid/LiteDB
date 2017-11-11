using System;
using System.Collections.Generic;

namespace LiteDB
{
    public sealed partial class LiteCollection<T>
    {
        private string _name;
        private LazyLoad<LiteEngine> _engine;
        private BsonMapper _mapper;
        private Logger _log;
        private List<string> _includes;
        private QueryVisitor<T> _visitor;
        private MemberMapper _id = null;
        private BsonType _autoId = BsonType.Null;

        /// <summary>
        /// Get collection name
        /// </summary>
        public string Name { get { return _name; } }

        /// <summary>
        /// Returns visitor resolver query only for internals implementations
        /// </summary>
        internal QueryVisitor<T> Visitor { get { return _visitor; } }

        public LiteCollection(string name, LazyLoad<LiteEngine> engine, BsonMapper mapper, Logger log)
        {
            _name = name ?? mapper.ResolveCollectionName(typeof(T));
            _engine = engine;
            _mapper = mapper;
            _log = log;
            _visitor = new QueryVisitor<T>(mapper);
            _includes = new List<string>();

            // if strong typed collection, get _id member mapped (if exists)
            if (typeof(T) != typeof(BsonDocument))
            {
                var entity = mapper.GetEntityMapper(typeof(T));
                _id = entity.Id;

                if (_id != null && _id.AutoId)
                {
                    _autoId =
                        _id.DataType == typeof(ObjectId) ? BsonType.ObjectId :
                        _id.DataType == typeof(Guid) ? BsonType.Guid :
                        _id.DataType == typeof(DateTime) ? BsonType.DateTime :
                        _id.DataType == typeof(Int32) ? BsonType.Int32 :
                        _id.DataType == typeof(Int64) ? BsonType.Int64 :
                        BsonType.Null;
                }
            }
            else
            {
                _autoId = BsonType.ObjectId;
            }
        }
    }
}