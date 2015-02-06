using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Storage is a special collection to store files/streams.
    /// </summary>
    public partial class LiteGridFS
    {
        public LiteCollection<BsonDocument> Files { get; private set; }
        public LiteCollection<BsonDocument> Chunks { get; private set; }
        public LiteDatabase Database { get; private set; }

        internal LiteGridFS(LiteDatabase db)
        {
            this.Database = db;
            this.Files = this.Database.GetCollection("_files");
            this.Chunks = this.Database.GetCollection("_chunks");
        }
    }
}
