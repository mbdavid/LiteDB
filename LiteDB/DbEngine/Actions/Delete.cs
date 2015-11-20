using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Implements delete based on a query result
        /// </summary>
        public int DeleteDocuments(string colName, Query query, int bufferSize)
        {
            return this.TransactionLoop<IndexNode>(colName, false, bufferSize, (c) => query.Run(c, _indexer), (col, node) => {

                _log.Write(Logger.COMMAND, "delete document on '{0}' :: _id = {1}", colName, node.Key);

                // read dataBlock (do not read all extend pages, i will not use)
                var dataBlock = _data.Read(node.DataBlock, false);

                // lets remove all indexes that point to this in dataBlock
                foreach (var index in col.GetIndexes(true))
                {
                    _indexer.Delete(index, dataBlock.IndexRef[index.Slot]);
                }

                // remove object data
                _data.Delete(col, node.DataBlock);

                return true;
            });
        }
    }
}
