using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Internal class to read all datafile documents - use only Stream - no cache system (database are modified during this read - shrink)
    /// </summary>
    internal class FileReaderV8 : IFileReader
    {
        private readonly Dictionary<string, uint> _collections;
        private readonly Dictionary<string, List<IndexInfo>> _indexes;
        private readonly Dictionary<string, BsonValue> _pragmas;
        private readonly IDictionary<uint, Tuple<ushort, long>> _logIndexMap = new Dictionary<uint, Tuple<ushort, long>>();
        private readonly Stream _stream;

        private readonly byte[] _cacheBuffer = new byte[PAGE_SIZE];
        private BasePage _cachePage;

        private bool _disposedValue;

        private readonly EngineSettings _settings;
        private readonly StringBuilder _errors;

        public IDictionary<string, BsonValue> GetPragmas() => _pragmas;

        public FileReaderV8(EngineSettings settings, StringBuilder errors)
        {
            _settings = settings;
            _errors = errors;
        }

        public void Open()
        {
            var dataFactory = _settings.CreateDataFactory();
            var logFactory = _settings.CreateLogFactory();

            if (logFactory.Exists())
            {
                this.LoadIndexMap(logFactory);
            }
        }

        private void LoadIndexMap(IStreamFactory logFactory)
        {
        }

        /// <summary>
        /// Check header slots to test if data file is a LiteDB FILE_VERSION = v8
        /// </summary>
        public static bool IsVersion(byte[] buffer)
        {
            var header = Encoding.UTF8.GetString(buffer, HeaderPage.P_HEADER_INFO, HeaderPage.HEADER_INFO.Length);
            var version = buffer[HeaderPage.P_FILE_VERSION];

            // buffer[0] = 1 when datafile is encrypted (this feature was added in v8 only)
            // all other version has this buffer[0] = 0

            return (header == HeaderPage.HEADER_INFO && version == HeaderPage.FILE_VERSION) ||
                buffer[0] == 1;
        }

        /// <summary>
        /// Read all collection based on header page
        /// </summary>
        public IEnumerable<string> GetCollections() => _collections.Keys;

        /// <summary>
        /// Read all indexes from all collection pages (except _id index)
        /// </summary>
        public IEnumerable<IndexInfo> GetIndexes(string collection) => _indexes[collection];

        /// <summary>
        /// Read all documents from current collection with NO index use - read direct from free lists
        /// There is no document order
        /// </summary>
        public IEnumerable<BsonDocument> GetDocuments(string collection)
        {
            return null;
        }

        /// <summary>
        /// Read page from stream - do not use cache system
        /// </summary>
        private T ReadPage<T>(uint pageID)
            where T : BasePage
        {
            var position = BasePage.GetPagePosition(pageID);

            if (_cachePage?.PageID == pageID) return (T)_cachePage;

            _stream.Position = position;
            _stream.Read(_cacheBuffer, 0, PAGE_SIZE);

            var buffer = new PageBuffer(_cacheBuffer, 0, 0);

            return (T)(_cachePage = BasePage.ReadPage<T>(buffer));
        }

        /// <summary>
        /// Get all data blocks from first data block
        /// </summary>
        public IEnumerable<BufferSlice> ReadBlocks(PageAddress address)
        {
            while (address != PageAddress.Empty)
            {
                var dataPage = this.ReadPage<DataPage>(address.PageID);

                var block = dataPage.GetBlock(address.Index);

                yield return block.Buffer;

                address = block.NextBlock;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                    // TODO: dispose managed state (managed objects)
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}