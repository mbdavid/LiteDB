using System;
using System.Linq;

namespace LiteDB
{
   public partial class DbEngine
    {
        /// <summary>
        /// Copy database do another disk
        /// </summary>
        public long Shrink()
        {
            // begin a write exclusive access
            using (var trans = _transaction.Begin(false))
            {
                try
                {

                    // create a temporary disk
                    var tempDisk = _disk.GetTempDisk();

                    // get initial disk size
                    var header = _pager.GetPage<HeaderPage>(0);
                    var diff = 0L;

                    // create temp engine instance to copy all documents
                    using (var tempEngine = new DbEngine(tempDisk, new Logger()))
                    {
                        tempDisk.Open(false);

                        // read all collections
                        foreach (var col in _collections.GetAll())
                        {
                            // first copy all indexes
                            foreach (var index in col.GetIndexes(false))
                            {
                                tempEngine.EnsureIndex(col.CollectionName, index.Field, index.Options);
                            }

                            // then, read all documents and copy to new engine
                            var nodes = _indexer.FindAll(col.PK, Query.Ascending);

                            tempEngine.Insert(col.CollectionName,
                                nodes.Select(node => BsonSerializer.Deserialize(_data.Read(node.DataBlock))));

                            // then re-open the disk service as the previous Insert's auto-transaction just closed it.
                            tempDisk.Open(false);
                        }

                        // get final header from temp engine
                        var tempHeader = tempEngine._pager.GetPage<HeaderPage>(0, true);

                        // copy info from initial header to final header
                        tempHeader.ChangeID = header.ChangeID;

                        // lets create journal file before re-write
                        for (uint pageID = 0; pageID <= header.LastPageID; pageID++)
                        {
                            _disk.WriteJournal(pageID, _disk.ReadPage(pageID));
                        }

                        // commit journal + shrink data file
                        _disk.SetLength(BasePage.GetSizeOfPages(tempHeader.LastPageID + 1));

                        // lets re-write all pages copying from new database
                        for (uint pageID = 0; pageID <= tempHeader.LastPageID; pageID++)
                        {
                            _disk.WritePage(pageID, tempDisk.ReadPage(pageID));
                        }

                        // now delete journal
                        _disk.DeleteJournal();

                        // get diff from initial and final last pageID
                        diff = BasePage.GetSizeOfPages(header.LastPageID - tempHeader.LastPageID);

                        tempDisk.Close();
                    }

                    // unlock disk and clear cache to continue
                    trans.Commit();

                    // delete temporary disk
                    _disk.DeleteTempDisk();

                    return diff;
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);
                    trans.Rollback();
                    throw;
                }
            }
        }
    }
}