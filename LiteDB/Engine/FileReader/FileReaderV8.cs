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

        private Stream _dataStream;
        private Stream _logStream;
        private readonly IDictionary<uint, long> _logIndexMap = new Dictionary<uint, long>();

        private readonly byte[] _cacheBuffer = new byte[PAGE_SIZE];
        private BasePage _cachePage;

        private bool _disposedValue;

        private readonly EngineSettings _settings;
        private readonly IList<FileReaderError> _errors;

        public IDictionary<string, BsonValue> GetPragmas() => _pragmas;

        public FileReaderV8(EngineSettings settings, IList<FileReaderError> errors)
        {
            _settings = settings;
            _errors = errors;
        }

        public void Open()
        {
            var dataFactory = _settings.CreateDataFactory();
            var logFactory = _settings.CreateLogFactory();

            _dataStream = dataFactory.GetStream(true, true, false);

            _dataStream.Position = 0;


            if (logFactory.Exists())
            {
                _logStream = logFactory.GetStream(false, false, true);

                this.LoadIndexMap();
            }
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
        /// Load log file to build index map (wal map index)
        /// </summary>
        private void LoadIndexMap()
        {
            var buffer = new PageBuffer(new byte[PAGE_SIZE], 0, 0);
            var transactions = new Dictionary<uint, List<PagePosition>>();
            var confirmedTransactions = new List<uint>();
            var currentPosition = 0L;

            _logStream.Position = 0;

            while (_logStream.Position < _logStream.Length)
            {
                if (buffer.IsBlank())
                {
                    // this should not happen, but if it does, it means there's a zeroed page in the file
                    // just skip it
                    currentPosition += PAGE_SIZE;
                    continue;
                }

                _logStream.Position = currentPosition;

                var read = _logStream.Read(buffer.Array, buffer.Offset, PAGE_SIZE);

                var pageID = buffer.ReadUInt32(BasePage.P_PAGE_ID);
                var isConfirmed = buffer.ReadBool(BasePage.P_IS_CONFIRMED);
                var transactionID = buffer.ReadUInt32(BasePage.P_TRANSACTION_ID);

                if (read != PAGE_SIZE)
                {
                    _errors.Add(new FileReaderError
                    {
                        Origin = FileOrigin.Log,
                        Position = _logStream.Position,
                        PageID = pageID,
                        Code = 1,
                        Message = $"Page position {_logStream} read only than {read} bytes (insted {PAGE_SIZE})"
                    });
                }

                var position = new PagePosition(pageID, currentPosition);

                if (transactions.TryGetValue(transactionID, out var list))
                {
                    list.Add(position);
                }
                else
                {
                    transactions[transactionID] = new List<PagePosition> { position };
                }

                // when page confirm transaction, add to confirmed transaction list
                if (isConfirmed)
                {
                    confirmedTransactions.Add(transactionID);
                }

                currentPosition += PAGE_SIZE;
            }

            // now, log index map using only confirmed transactions (override with last transactionID)
            foreach (var transactionID in confirmedTransactions)
            {
                var mapIndexPages = transactions[transactionID];

                // update 
                foreach (var page in mapIndexPages)
                {
                    _logIndexMap[page.PageID] = page.Position;
                }
            }

        }

        /// <summary>
        /// Read page from stream
        /// </summary>
        private T ReadPage<T>(uint pageID)
            where T : BasePage
        {
            // get data from log file or original file
            if (_logIndexMap.TryGetValue(pageID, out var position))
            {
                _logStream.Position = position;
                _logStream.Read(_cacheBuffer, 0, PAGE_SIZE);
            }
            else
            {
                _dataStream.Position = BasePage.GetPagePosition(pageID);
                _dataStream.Read(_cacheBuffer, 0, PAGE_SIZE);
            }
            var pageBuffer = new PageBuffer(_cacheBuffer, 0, 0);

            return BasePage.ReadPage<T>(pageBuffer);
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