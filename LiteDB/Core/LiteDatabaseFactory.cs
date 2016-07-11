using System.IO;
using Android.Runtime;
using LiteDB.Core;
using LiteDB.Interfaces;

namespace LiteDB
{
   [Preserve]
   public class LiteDatabaseFactory
   {
      private static Logger _log = new Logger();

      /// <summary>
      /// Starts LiteDB database using a connection string for filesystem database
      /// </summary>
      public static ILiteDatabase Create(string connectionString, BsonMapper mapper = null)
      {
         return Create<LiteDatabase>(connectionString);
      }

      /// <summary>
      /// Initialize database using any read/write Stream (like MemoryStream)
      /// </summary>
      public static ILiteDatabase Create(Stream stream, BsonMapper mapper = null)
      {
         return Create<LiteDatabase>(stream, mapper);
      }

      /// <summary>
      /// Starts LiteDB database using a connection string for filesystem database
      /// </summary>
      public static T Create<T>(string connectionString, BsonMapper mapper = null) where T : ILiteDatabase, new()
      {
         var conn = new ConnectionString(connectionString);

         var db = new T();
         db.CreateEngine(LiteDbPlatform.Platform.CreateFileDiskService(conn, _log), mapper);

         return db;
      }

      /// <summary>
      /// Initialize database using any read/write Stream (like MemoryStream)
      /// </summary>
      public static T Create<T>(Stream stream, BsonMapper mapper = null) where T : ILiteDatabase, new()
      {
         var db = new T();
         db.CreateEngine(new StreamDiskService(stream), mapper);

         return db;
      }
   }
}