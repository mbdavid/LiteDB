using System;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Reduce disk size rearranging unused spaces.
        /// </summary>
        public long Shrink(string password = null)
        {
            var originalSize = _disk.FileLength;

            IDiskService temp = null;

            using (var engine = new LiteEngine(temp))
            {
                // read all collection
                foreach (var collectionName in this.GetCollectionNames())
                {
                    // first create all user indexes (exclude _id index)
                    foreach (var index in this.GetIndexes(collectionName).Where(x => x.Field != "_id"))
                    {
                        engine.EnsureIndex(collectionName, index.Field, index.Unique);
                    }

                    // copy all docs
                    engine.Insert(collectionName, this.Find(collectionName, Query.All()));
                }

                // copy user version
                engine.UserVersion = this.UserVersion;

                // set current disk size to exact new disk usage
                _disk.SetLength(temp.FileLength);

                // read new header page to start copy
                var header = BasePage.ReadPage(temp.ReadPage(0)) as HeaderPage;

                // copy all pages from temp disk to original disk
                for (uint i = 0; i <= header.LastPageID; i++)
                {
                    var page = temp.ReadPage(i);

                    _disk.WritePage(i, page);
                }
            }

            // return how many bytes are reduced
            return originalSize - temp.FileLength;
        }
    }
}