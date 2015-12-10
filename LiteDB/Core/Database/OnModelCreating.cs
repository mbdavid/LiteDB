using System;
using System.Collections.Generic;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        /// <summary>
        /// Use mapper cache
        /// </summary>
        private static Dictionary<Type, BsonMapper> _mapperCache = new Dictionary<Type, BsonMapper>();

        private void InitializeMapper()
        {
            lock (_mapperCache)
            {
                if (!_mapperCache.TryGetValue(this.GetType(), out _mapper))
                {
                    _mapper = new BsonMapper();
                    _mapperCache.Add(this.GetType(), _mapper);
                    this.OnModelCreating(_mapper);
                }
            }
        }

        /// <summary>
        /// Use this method to override and apply rules to map your entities to BsonDocument
        /// </summary>
        protected virtual void OnModelCreating(BsonMapper mapper)
        {
        }
    }
}