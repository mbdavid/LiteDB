using System;
using System.Collections.Generic;
using LiteDB;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Copy database do another disk
        /// </summary>
        public int Shrink(IDiskService tempDisk)
        {
            // lock and clear cache - no changes during shrink
            _disk.Lock();
            _cache.Clear();

            // get initial disk size
            var header = _pager.GetPage<HeaderPage>(0);
            var diff = 0;

            // create temp engine instance to copy all documents
            using (var tempEngine = new DbEngine(tempDisk, new Logger()))
            {
                // read all collections 
                foreach (var col in _collections.GetAll())
                {
                    // first copy all indexes
                    foreach(var index in col.GetIndexes(false))
                    {
                        tempEngine.EnsureIndex(col.CollectionName, index.Field, index.Options);
                    }

                    // then, read all documents and copy to new engine
                    var docs = _indexer.FindAll(col.PK, Query.Ascending);

                    tempEngine.InsertDocuments(col.CollectionName, 
                        docs.Select(x => BsonSerializer.Deserialize(_data.Read(x.DataBlock, true).Buffer)));
                }

                // get final header from temp engine
                var tempHeader = tempEngine._pager.GetPage<HeaderPage>(0);

                // copy info from initial header to final header
                tempHeader.ChangeID = header.ChangeID;
                tempHeader.UserVersion = header.UserVersion;

                // lets create journal file before re-write
                for (uint pageID = 0; pageID <= header.LastPageID; pageID++)
                {
                    _disk.WriteJournal(pageID, _disk.ReadPage(pageID));
                }

                // commit journal + shrink data file
                _disk.CommitJournal((tempHeader.LastPageID + 1) * BasePage.PAGE_SIZE);

                // lets re-write all pages copying from new database
                for (uint pageID = 0; pageID <= tempHeader.LastPageID; pageID++)
                {
                    _disk.WritePage(pageID, tempDisk.ReadPage(pageID));
                }

                // now delete journal
                _disk.DeleteJournal();

                // get diff from initial and final last pageID
                diff = (int)((header.LastPageID - tempHeader.LastPageID) * BasePage.PAGE_SIZE);
            }

            // unlock disk to continue
            _disk.Unlock();

            return diff;
        }
    }
}
