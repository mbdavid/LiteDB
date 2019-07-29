using LiteDB.Engine;
using System;
using System.Collections.Generic;
using static LiteDB.Constants;

namespace LiteDB
{
    public sealed partial class LiteCollection<T>
    {
        private readonly string _collection;
        private readonly Lazy<ILiteEngine> _engine;
        private readonly BsonMapper _mapper;
        private readonly List<BsonExpression> _includes;
        private readonly MemberMapper _id;
        private readonly BsonAutoId _autoId;

        /// <summary>
        /// Get collection name
        /// </summary>
        public string Name => _collection;

        internal LiteCollection(string name, BsonAutoId autoId, Lazy<ILiteEngine> engine, BsonMapper mapper)
        {
            _collection = name ?? mapper.ResolveCollectionName(typeof(T));
            _engine = engine;
            _mapper = mapper;
            _includes = new List<BsonExpression>();

            // if strong typed collection, get _id member mapped (if exists)
            if (typeof(T) == typeof(BsonDocument))
            {
                _id = null;
                _autoId = autoId;
            }
            else
            {
                var entity = mapper.GetEntityMapper(typeof(T));
                _id = entity.Id;

                if (_id != null && _id.AutoId)
                {
                    _autoId =
                        _id.DataType == typeof(Int32) || _id.DataType == typeof(Int32?) ? BsonAutoId.Int32 :
                        _id.DataType == typeof(Int64) || _id.DataType == typeof(Int64?) ? BsonAutoId.Int64 :
                        _id.DataType == typeof(Guid) || _id.DataType == typeof(Guid?) ? BsonAutoId.Guid :
                        BsonAutoId.ObjectId;
                }
            }
        }
    }
}