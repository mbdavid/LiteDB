using System;
using System.Collections.Generic;

namespace LiteDB.Interfaces
{
   public interface ILiteDatabase : IDisposable
   {
      void CreateEngine(IDiskService diskService, BsonMapper mapper = null);

      LiteCollection<T> GetCollection<T>(string name) where T : new();
      LiteCollection<BsonDocument> GetCollection(string name);
      bool CollectionExists(string name);
      bool DropCollection(string name);

      ushort DbVersion { get; set; }

      LiteFileStorage FileStorage { get; }
      DbEngine Engine { get; }
      Logger Log { get; }
      IEnumerable<string> GetCollectionNames();
   }
}