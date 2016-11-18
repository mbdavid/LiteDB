using System;

namespace LiteDB
{
    public partial class LiteDatabase
    {
        /// <summary>
        /// Reduce datafile size re-creating all collection in another datafile - return how many bytes are reduced.
        /// </summary>
        public long Shrink()
        {
            return _engine.Value.Shrink();
        }

        /// <summary>
        /// Convert a BsonDocument to a class object using BsonMapper rules
        /// </summary>
        public T ToObject<T>(BsonDocument doc)
            where T : new()
        {
            return _mapper.ToObject<T>(doc);
        }

        /// <summary>
        /// Convert a BsonDocument to a class object using BsonMapper rules
        /// </summary>
        public object ToObject(Type type, BsonDocument doc)
        {
            return _mapper.ToObject(type, doc);
        }

        /// <summary>
        /// Convert an entity class instance into a BsonDocument using BsonMapper rules
        /// </summary>
        public BsonDocument ToDocument<T>(T entity)
        {
            return _mapper.ToDocument(entity);
        }

        /// <summary>
        /// Convert an entity class instance into a BsonDocument using BsonMapper rules
        /// </summary>
        public BsonDocument ToDocument(Type type, object entity)
        {
            return _mapper.ToDocument(type, entity);
        }
    }
}