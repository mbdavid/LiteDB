using System;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        /// <summary>
        /// Reduce datafile size re-creating all collection in another datafile - return how many bytes are reduced.
        /// </summary>
        public int Shrink()
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
        /// Convert an entity class instance into a BsonDocument using BsonMapper rules
        /// </summary>
        public BsonDocument ToDocument(object entity)
        {
            return _mapper.ToDocument(entity);
        }
    }
}