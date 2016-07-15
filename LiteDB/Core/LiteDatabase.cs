using System.IO;
using LiteDB.Core;
using LiteDB.Interfaces;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resoures. It's the database connection
    /// </summary>
    public partial class LiteDatabase : ILiteDatabase
   {
        private LazyLoad<DbEngine> _engine;

        private BsonMapper _mapper;

        private Logger _log = new Logger();

        public Logger Log { get { return _log; } }

       public DbEngine Engine
       {
          get { return _engine.Value; }
       }
      
      /// <summary>
      /// Starts LiteDB database using a connection string for filesystem database
      /// </summary>
      public LiteDatabase(string connectionString, BsonMapper mapper = null)
      {
         var conn = new ConnectionString(connectionString);
         CreateEngine(LiteDbPlatform.Platform.CreateFileDiskService(conn, _log), mapper);
      }

      /// <summary>
      /// Starts LiteDB database using a custom IDiskService
      /// </summary>
      public LiteDatabase(IDiskService diskService, BsonMapper mapper = null)
      {
         CreateEngine(diskService, mapper);
      }

       /// <summary>
       /// Initialize database using any read/write Stream (like MemoryStream)
       /// </summary>
       public LiteDatabase(Stream stream, BsonMapper mapper = null)
       {
         CreateEngine(new StreamDiskService(stream), mapper);
      }

      public void CreateEngine(IDiskService diskService, BsonMapper mapper = null)
      {
         LiteDbPlatform.ThrowIfNotInitialized();

         _mapper = mapper ?? BsonMapper.Global;
         _engine = new LazyLoad<DbEngine>(() => new DbEngine(diskService, _log));
      }

      /// <summary>
      /// Get/Set database version
      /// </summary>
      public ushort DbVersion
      {
         get { return _engine.Value.ReadDbVersion(); }
         set { _engine.Value.WriteDbVersion(value); }
      }

      public void Dispose()
        {
            if (_engine.IsValueCreated) _engine.Value.Dispose();
        }
    }
}