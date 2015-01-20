using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class Collection<T>
    {
        public BsonObject Stats()
        {
            _engine.Transaction.AvoidDirtyRead();

            var col = this.GetCollectionPage(false);

            if(col == null) return new BsonObject();

            var stats = new BsonObject();

            stats["name"] = col.CollectionName;
            stats["documents"] = col.DocumentCount;

            //TODO: Não tenho como fazer de forma eficiente.
            //  Não tenho um link de todas as paginas de uma mesma colecao
            //  Somente quando implementar os Drop/DropCollection corretos vou conseguir isso
            //  

            // db.collection.dataSize(): data size in bytes for the collection.
            // db.collection.storageSize(): allocation size in bytes, including unused space.
            // db.collection.totalSize(): the data size plus the index size in bytes.
            // db.collection.totalIndexSize(): the index size in bytes.


            // indexes
            stats["indexes"] = new BsonArray();

            for (var i = 0; i < col.Indexes.Length; i++)
            {
                var index = col.Indexes[i];

                if (index.IsEmpty) continue;

                var idx = new BsonObject();
                idx["field"] = index.Field;
                idx["unique"] = index.Unique;

                //     nullCount
                //     avg key size
                //     % (0.00-1.00) distinct > quanto mais proximo do 1 melhor, mais seletivo


                stats["indexes"].AsArray.Add(idx);
            }

            return stats;
        }
    }
}
