using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly Dictionary<string, uint> _collections = new Dictionary<string, uint>();
        private readonly Dictionary<string, List<IndexInfo>> _indexes = new Dictionary<string, List<IndexInfo>>();
        private readonly Dictionary<string, BsonValue> _pragmas = new Dictionary<string, BsonValue>();
        private readonly Dictionary<uint, List<uint>> _collectionsDataPages = new Dictionary<uint, List<uint>>();

        private Stream _dataStream;
        private Stream _logStream;
        private readonly IDictionary<uint, long> _logIndexMap = new Dictionary<uint, long>();

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

            this.LoadPragmas();

            this.LoadCollections();



        }

        /// <summary>
        /// Load all pragmas from header page
        /// </summary>
        private void LoadPragmas()
        {
            var origin = FileOrigin.None;
            var position = 0L;

            try
            {
                var header = this.ReadPage(0, out origin, out position);

                var buffer = header.Buffer;

                _pragmas[Pragmas.USER_VERSION] = buffer.ReadInt32(EnginePragmas.P_USER_VERSION);
                _pragmas[Pragmas.CHECKPOINT] = buffer.ReadInt32(EnginePragmas.P_CHECKPOINT);
                _pragmas[Pragmas.TIMEOUT] = buffer.ReadInt32(EnginePragmas.P_TIMEOUT);
                _pragmas[Pragmas.UTC_DATE] = buffer.ReadBool(EnginePragmas.P_UTC_DATE);
                _pragmas[Pragmas.LIMIT_SIZE] = buffer.ReadInt64(EnginePragmas.P_LIMIT_SIZE);
            }
            catch (Exception ex)
            {
                _errors.Add(new FileReaderError
                {
                    Origin = origin,
                    Position = position,
                    PageID = 0,
                    Code = 1,
                    Message = $"Header pragmas could not be loaded",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Read all file (and log) to find all data pages (and store groupby colPageID)
        /// </summary>
        private void LoadDataPages()
        {
            var header = ReadPage(0, out _, out _);

            var lastPageID = header.Buffer.ReadUInt32(HeaderPage.P_LAST_PAGE_ID);

            for (uint i = 0; i < lastPageID; i++)
            {
                var page = ReadPage(i, out _, out _);

                if (page.PageType == PageType.Data)
                {
                    if (_collectionsDataPages.TryGetValue(page.ColID, out var list))
                    {
                        list.Add(page.PageID);
                    }
                    else
                    {
                        _collectionsDataPages[page.ColID] = new List<uint> { page.PageID };
                    }
                }
            }
        }

        /// <summary>
        /// Load all collections from header OR via all data-pages ColID
        /// </summary>
        private void LoadCollections()
        {
            try
            {
                var header = this.ReadPage(0, out _, out _);

                var area = header.Buffer.Slice(HeaderPage.P_COLLECTIONS, HeaderPage.COLLECTIONS_SIZE);

                using (var r = new BufferReader(new[] { area }, false))
                {
                    var collections = r.ReadDocument();

                    foreach (var key in collections.Keys)
                    {
                        // collections.key = collection name
                        // collections.value = collection PageID
                        _collections[key] = (uint)collections[key].AsInt32;
                    }
                }

                // for each collection loaded by datapages, check if exists in _collections
                foreach(var collection in _collectionsDataPages)
                {
                    if (!_collections.ContainsValue(collection.Key))
                    {
                        _collections["col_" + collection.Key] = collection.Key;
                    }
                }

            }
            catch (Exception ex)
            {

            }

        }

        /// <summary>
        /// Load all indexes for all collections
        /// </summary>
        private void LoadIndexes()
        {
            foreach(var collection in _collections)
            {
                var page = ReadPage(collection.Value, out _, out _);
                var buffer = page.Buffer;

                var count = buffer.ReadByte(CollectionPage.P_INDEXES); // 1 byte
                var position = CollectionPage.P_INDEXES + 1;

                for (var i = 0; i < count; i++)
                {
                    position += 2; // skip: slot (1 byte) + indexType (1 byte)
                    
                    var name = buffer.ReadCString(position, out var nameLength);
                    // depois de ler, validar se a position ainda é válida (se é < 8192)
                    // validar o tamanho do nome do índice para ver se o nome lido é válido

                    position += nameLength;

                    var expr = buffer.ReadCString(position, out var exprLength);
                    // depois de ler, validar se a position ainda é válida (se é < 8192)
                    // validar se a expr é válida

                    position += exprLength;

                    var unique = buffer.ReadBool(position);
                    // depois de ler, validar se a position ainda é válida (se é < 8192)

                    position += 15; // head 5 bytes, tail 5 bytes, maxLevel 1 byte, freeIndexPageList 4 bytes

                    var indexInfo = new IndexInfo
                    {
                        Collection = collection.Key,
                        Name = name,
                        Expression = expr,
                        Unique = unique
                    };

                    // ignore _id index
                    if (name == "_id") continue;

                    if (_indexes.TryGetValue(collection.Key, out var indexInfos))
                    {
                        indexInfos.Add(indexInfo);
                    }
                    else
                    {
                        _indexes[collection.Key] = new List<IndexInfo> { indexInfo };
                    }
                }
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
            var colPageID = _collections[collection];
            var dataPages = _collectionsDataPages[colPageID];

            // varrer tudo a partir dos dataPages

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
                        Message = $"Page position {_logStream} read only than {read} bytes (instead {PAGE_SIZE})"
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
        private BasePage ReadPage(uint pageID, out FileOrigin origin, out long position)
        {
            var pageBuffer = new PageBuffer(new byte[PAGE_SIZE], 0, PAGE_SIZE);

            // get data from log file or original file
            if (_logIndexMap.TryGetValue(pageID, out position))
            {
                origin = FileOrigin.Log;

                _logStream.Position = position;
                _logStream.Read(pageBuffer.Array, pageBuffer.Offset, pageBuffer.Count);
            }
            else
            {
                origin = FileOrigin.Data;
                position = BasePage.GetPagePosition(pageID);

                _dataStream.Position = position;
                _dataStream.Read(pageBuffer.Array, pageBuffer.Offset, pageBuffer.Count);
            }

            var page = new BasePage(pageBuffer);

            return page;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _dataStream?.Dispose();
                _logStream?.Dispose();
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