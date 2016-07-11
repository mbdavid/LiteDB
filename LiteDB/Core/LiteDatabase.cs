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

       public LiteDatabase()
      {

      }

      public LiteDatabase(IDiskService diskService, BsonMapper mapper = null)
      {
         CreateEngine(diskService, mapper);
      }

      public void CreateEngine(IDiskService diskService, BsonMapper mapper = null)
      {
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