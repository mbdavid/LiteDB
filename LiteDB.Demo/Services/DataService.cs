using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    internal class DataService : IDisposable
    {
        private FileStreamFactory _factory;
        private StreamPool _pool;
        private MemoryFile _file;
        private MemoryFileReader _reader;

        private uint _lastPageID = 0;
        private Dictionary<uint, DataPage> _local = new Dictionary<uint, DataPage>();

        public DataService(string path)
        {
            _factory = new FileStreamFactory(path, DbFileMode.Logfile, false);
            _pool = new StreamPool(_factory);
            _file = new MemoryFile(_pool, null);
            _reader = _file.GetReader(true);
        }

        private DataPage GetNewPage(int minBytes)
        {
            var freeBlocks = (byte)((minBytes / 32) + 1);

            // check if there is pages in my dirty list
            var p = _local.Values.FirstOrDefault(x => x.FreeBlocks >= freeBlocks);

            if (p == null)
            {
                p = new DataPage(_reader.NewPage(true), _lastPageID++);

                _local[p.PageID] = p;
            }

            p.IsDirty = true;

            return p;
        }

        private DataPage GetPage(uint pageID, bool markAsDirty)
        {
            if(_local.TryGetValue(pageID, out var page))
            {
                if (markAsDirty) page.IsDirty = true;

                return page;
            }

            page = new DataPage(_reader.GetPage(pageID * 8192, true));
            _local[pageID] = page;
            if (markAsDirty) page.IsDirty = true;
            return page;
        }

        public DataBlock Insert(BsonDocument doc)
        {
            var bytesLeft = doc.GetBytesCount(true);

            if (bytesLeft > 2 * 1024 * 1024) throw new Exception("too big - 2mb max");

            DataBlock firstBlock = null;

            IEnumerable<BufferSlice> source()
            {
                byte dataIndex = 0;
                DataBlock lastBlock = null;

                while (bytesLeft > 0)
                {
                    var bytesToCopy = Math.Min(bytesLeft, (254 * 32) - 7); // 254 blocos - 6 blockHeader - 1 segmentHeader
                    var dataPage = this.GetNewPage(bytesToCopy);
                    var dataBlock = dataPage.InsertBlock(bytesToCopy, dataIndex);

                    dataIndex++;

                    if (lastBlock != null)
                    {
                        lastBlock.UpdateNextBlock(dataBlock.Position);
                    }

                    if (firstBlock == null) firstBlock = dataBlock;

                    yield return dataBlock.Buffer;

                    lastBlock = dataBlock;

                    bytesLeft -= bytesToCopy;
                }
            }

            using (var w = new BufferWriter(source()))
            {
                w.WriteDocument(doc);
            }

            return firstBlock;
        }

        public BsonDocument Read(PageAddress address)
        {
            IEnumerable<BufferSlice> source()
            {
                while(address != PageAddress.Empty)
                {
                    var dataPage = this.GetPage(address.PageID, false);

                    var block = dataPage.ReadBlock(address.Index);

                    yield return block.Buffer;

                    address = block.NextBlock;
                }
            }

            using (var r = new BufferReader(source(), true))
            {
                return r.ReadDocument();
            }
        }

        public void Delete(PageAddress address)
        {
            while (address != PageAddress.Empty)
            {
                var dataPage = this.GetPage(address.PageID, true);
                var block = dataPage.DeleteBlock(address.Index);

                address = block.NextBlock;
            }
        }

        public void Update(PageAddress address, BsonDocument doc)
        {
            var bytesLeft = doc.GetBytesCount(true);

            if (bytesLeft > 2 * 1024 * 1024) throw new Exception("too big - 2mb max");

            IEnumerable<BufferSlice> source()
            {
                byte dataIndex = 0;
                DataBlock lastBlock = null;

                while (bytesLeft > 0)
                {
                    var bytesToCopy = 0;
                    DataBlock dataBlock;

                    // new page
                    if (address == PageAddress.Empty)
                    {
                        bytesToCopy = Math.Min(bytesLeft, (254 * 32) - 7); // 254 blocos - 6 blockHeader - 1 segmentHeader
                        var dataPage = this.GetNewPage(bytesToCopy);

                        dataBlock = dataPage.InsertBlock(bytesToCopy, dataIndex);
                    }
                    // update current page
                    else
                    {
                        var dataPage = this.GetPage(address.PageID, true);
                        var currentBlock = dataPage.ReadBlock(address.Index);
                        var spaceInPage = (dataPage.FreeBlocks * 32) + currentBlock.Buffer.Count;

                        bytesToCopy = Math.Min(bytesLeft, spaceInPage);
                        dataBlock = dataPage.UpdateBlock(currentBlock, bytesToCopy);
                    }

                    dataIndex++;

                    if (lastBlock != null)
                    {
                        lastBlock.UpdateNextBlock(dataBlock.Position);
                    }

                    yield return dataBlock.Buffer;

                    lastBlock = dataBlock;

                    address = dataBlock.NextBlock;

                    bytesLeft -= bytesToCopy;
                }

                // update last block with no next block
                lastBlock.UpdateNextBlock(PageAddress.Empty);

                // delete extra datablock
                this.Delete(address);
            }

            using (var w = new BufferWriter(source()))
            {
                w.WriteDocument(doc);

                // force consume all source enumerable
                w.Consume();
            }
        }

        public void Dispose()
        {
            _file.WriteAsync(_local.Values.Where(x => x.IsDirty).Select(x => x.UpdateBuffer()));
            _reader.Dispose();
            _file.Dispose();
        }
    }
}
