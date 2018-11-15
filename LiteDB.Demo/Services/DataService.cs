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
        private List<DataPage> _dirty = new List<DataPage>();
        private Dictionary<uint, DataPage> _local = new Dictionary<uint, DataPage>();

        public DataService(string path)
        {
            _factory = new FileStreamFactory(path, false);
            _pool = new StreamPool(_factory, DbFileMode.Logfile);
            _file = new MemoryFile(_pool, null);
            _reader = _file.GetReader(true);
        }

        private DataPage GetDirtyPage(int minBytes)
        {
            var freeBlocks = (byte)((minBytes / 32) + 1);

            // check if there is pages in my dirty list
            var p = _dirty.FirstOrDefault(x => x.FreeBlocks >= freeBlocks);

            if (p == null)
            {
                p = new DataPage(_reader.NewPage(), _lastPageID++);

                _dirty.Add(p);
            }

            return p;
        }

        private DataPage GetCleanPage(uint pageID)
        {
            if(_local.TryGetValue(pageID, out var page))
            {
                return page;
            }

            page = new DataPage(_reader.GetPage(pageID * 8192));
            _local[pageID] = page;
            return page;
        }

        public DataBlock Insert(BsonDocument doc)
        {
            var bytesLeft = doc.GetBytesCount(true);

            if (bytesLeft > 2 * 1024 * 1024) throw new Exception("too big - 2mb max");

            DataBlock first = null;

            IEnumerable<BufferSlice> source()
            {
                byte dataIndex = 0;
                DataBlock last = null;

                while (bytesLeft > 0)
                {
                    var bytesToCopy = Math.Min(bytesLeft, 8000);
                    var dataPage = this.GetDirtyPage(bytesToCopy);
                    var dataBlock = dataPage.InsertBlock(bytesToCopy, dataIndex);

                    dataIndex++;

                    if (last != null)
                    {
                        last.UpdateNextBlock(dataBlock.Position);
                    }

                    if (first == null) first = dataBlock;

                    yield return dataBlock.Buffer;

                    last = dataBlock;

                    bytesLeft -= bytesToCopy;
                }
            }

            using (var w = new BufferWriter(source()))
            {
                w.WriteDocument(doc);
            }

            return first;

        }

        public BsonDocument Read(PageAddress dataBlock)
        {
            IEnumerable<BufferSlice> source()
            {
                while(dataBlock != PageAddress.Empty)
                {
                    var dataPage = this.GetCleanPage(dataBlock.PageID);

                    var block = dataPage.ReadBlock(dataBlock.Index);

                    yield return block.Buffer;

                    dataBlock = block.NextBlock;
                }
            }

            using (var r = new BufferReader(source(), true))
            {
                return r.ReadDocument();
            }
        }

        public void Dispose()
        {
            _file.WriteAsync(_dirty.Select(x => x.UpdateBuffer()));
            _reader.Dispose();
            _file.Dispose();
        }
    }
}
