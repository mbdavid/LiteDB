//using System.Collections.Generic;
//using static LiteDB.Constants;

//namespace LiteDB.Engine
//{
//    /// <summary>
//    /// Same as DocumentLoad, but with cache to re-use same document if already loaded (with limit size)
//    /// </summary>
//    internal class CachedDocumentLoader : DocumentLoader
//    {
//        private readonly Dictionary<PageAddress, BsonDocument> _cache;

//        public CachedDocumentLoader(DataService data, bool utcDate, HashSet<string> fields)
//            : base (data, utcDate, fields)
//        {
//            _cache = new Dictionary<PageAddress, BsonDocument>();
//        }

//        public override BsonDocument Load(IndexNode node)
//        {
//            if(_cache.TryGetValue(node.DataBlock, out var doc))
//            {
//                return doc;
//            }
//            else if (_cache.Count > MAX_CACHE_DOCUMENT_LOADER_SIZE)
//            {
//                // do full cache clear if reach size limit
//                _cache.Clear();
//            }

//            return _cache[node.DataBlock] = doc = base.Load(node);
//        }
//    }
//}