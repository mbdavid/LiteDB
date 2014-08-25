using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class FilesCollection
    {
        private Collection<BsonDocument> _col;
        private LiteEngine _engine;

        internal FilesCollection(LiteEngine engine)
        {
            _engine = engine;
            _col = _engine.GetCollection("_files");
        }
    }
}
