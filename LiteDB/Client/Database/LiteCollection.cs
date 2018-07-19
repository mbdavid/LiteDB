using LiteDB.Engine;
using System;
using System.Collections.Generic;

namespace LiteDB
{
    public sealed partial class LiteCollection<T>
    {
        private readonly string _collection;
        private readonly Lazy<LiteEngine> _engine;
        private readonly BsonMapper _mapper;
        private readonly List<BsonExpression> _includes;
        private readonly QueryVisitor<T> _visitor;
        private readonly MemberMapper _id = null;
        private readonly BsonAutoId _autoId = BsonAutoId.ObjectId;

        /// <summary>
        /// Get collection name
        /// </summary>
        public string Name => _collection;

        /// <summary>
        /// Returns visitor resolver query only for internals implementations
        /// </summary>
        internal QueryVisitor<T> Visitor => _visitor;

        internal LiteCollection(string name, Lazy<LiteEngine> engine, BsonMapper mapper)
        {
            _collection = name ?? mapper.ResolveCollectionName(typeof(T));
            _engine = engine;
            _mapper = mapper;
            _visitor = new QueryVisitor<T>(mapper);
            _includes = new List<BsonExpression>();

            // if strong typed collection, get _id member mapped (if exists)
            if (typeof(T) != typeof(BsonDocument))
            {
                var entity = mapper.GetEntityMapper(typeof(T));
                _id = entity.Id;

                if (_id != null && _id.AutoId)
                {
                    _autoId =
                        _id.DataType == typeof(Int32) ? BsonAutoId.Int32 :
                        _id.DataType == typeof(Int64) ? BsonAutoId.Int64 :
                        _id.DataType == typeof(Guid) ? BsonAutoId.Guid :
                        _id.DataType == typeof(DateTime) ? BsonAutoId.DateTime :
                        BsonAutoId.ObjectId;
                }
            }
            else
            {
                _autoId = BsonAutoId.ObjectId;
            }
        }
    }
}