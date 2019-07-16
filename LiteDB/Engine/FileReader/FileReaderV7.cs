using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Internal class to read old LiteDB v4 database version (datafile v7 structure)
    /// </summary>
    internal class FileReaderV7 : IFileReader
    {
        // v7 uses 4k page size
        private const int V7_PAGE_SIZE = 4096;

        private readonly Stream _stream;
        private readonly AesEncryption _aes;

        private byte[] _buffer = new byte[V7_PAGE_SIZE];

        public int UserVersion { get; set; }

        public FileReaderV7(Stream stream, string password)
        {
            _stream = stream;

            // only userVersion was avaiable in old file format versions
            var header = this.ReadPage(0);

            this.UserVersion = header["userVersion"].AsInt32;

            if (password == null && header["salt"].AsBinary.IsFullZero() == false)
            {
                throw new LiteException(0, "Current datafile requires password");
            }
            else if (password != null)
            {
                if (header["salt"].AsBinary.IsFullZero())
                {
                    throw new LiteException(0, "Current datafile has no encryption - do not use password");
                }

                var hash = AesEncryption.HashSHA1(password);

                if (hash.SequenceEqual(header["password"].AsBinary) == false)
                {
                    throw new LiteException(0, "Invalid database password");
                }
            }

            _aes = password == null ?
                null :
                new AesEncryption(password, header["salt"].AsBinary);
        }

        /// <summary>
        /// Read all collection based on header page
        /// </summary>
        public IEnumerable<string> GetCollections()
        {
            var header = this.ReadPage(0);
            var names = header["collections"].AsArray.Select(x => x["name"].AsString).ToArray();

            return names;
        }

        /// <summary>
        /// Read all indexes from all collection pages
        /// </summary>
        public IEnumerable<IndexInfo> GetIndexes()
        {
            var header = this.ReadPage(0);

            foreach(var col in header["collections"].AsArray)
            {
                var page = this.ReadPage((uint)col["pageID"].AsInt32);

                foreach(var index in page["indexes"].AsArray)
                {
                    yield return new IndexInfo
                    {
                        Collection = col["name"].AsString,
                        Name = index["name"].AsString,
                        Expression = index["expression"].AsString,
                        Unique = index["unique"].AsBoolean,
                        HeadPageID = (uint)index["headPageID"].AsInt32
                    };
                }
            }
        }

        /// <summary>
        /// Get all document using an indexInfo as start point (_id index).
        /// </summary>
        public IEnumerable<BsonDocument> GetDocuments(IndexInfo index)
        {
            var indexPage = this.ReadPage(index.HeadPageID);
            var node = indexPage["nodes"][0].AsDocument;

            while (true)
            {
                var dataBlock = node["dataBlock"];
                var next = node["next"];

                // if datablock link to a data page
                if (dataBlock["pageID"].AsInt32 != -1)
                {
                    // read dataPage and data block
                    var dataPage = this.ReadPage((uint)dataBlock["pageID"].AsInt32);
                    var block = dataPage["blocks"].AsArray.Single(x => x["index"] == dataBlock["index"]).AsDocument;

                    // read byte[] from block or from extend pages
                    var data = block["extendPageID"] == -1 ?
                        block["data"].AsBinary :
                        this.ReadExtendData((uint)block["extendPageID"].AsInt32);

                    // BSON format still same from all version
                    var doc = BsonSerializer.Deserialize(data);

                    yield return doc;
                }

                // if no more index node, exit
                if (next["pageID"].AsInt32 == -1) break;

                // read next indexNode
                indexPage = this.ReadPage((uint)next["pageID"].AsInt32);
                node = indexPage["nodes"].AsArray.Single(x => x["index"] == next["index"]).AsDocument;
            }
        }

        /// <summary>
        /// Read all database pages from v7 structure into a flexible BsonDocument - only read what really needs
        /// </summary>
        private BsonDocument ReadPage(uint pageID)
        {
            _stream.Position = pageID * V7_PAGE_SIZE; // v7 uses 4k page size

            _stream.Read(_buffer, 0, V7_PAGE_SIZE);

            // decrypt encrypted page (except header page - header are plain data)
            if (_aes != null && pageID > 0)
            {
                _buffer = _aes.Decrypt(_buffer);
            }

            var reader = new ByteReader(_buffer);

            // reading page header
            var page = new BsonDocument
            {
                ["pageID"] = (int)reader.ReadUInt32(),
                ["pageType"] = (int)reader.ReadByte(),
                ["prevPageID"] = (int)reader.ReadUInt32(),
                ["nextPageID"] = (int)reader.ReadUInt32(),
                ["itemCount"] = (int)reader.ReadUInt16()
            };

            // skip freeByte + reserved
            reader.ReadBytes(2 + 8);

            #region Header (1)

            // read header
            if (page["pageType"] == 1)
            {
                var info = reader.ReadString(27);
                var ver = reader.ReadByte();

                if (string.CompareOrdinal(info, HeaderPage.HEADER_INFO) != 0 || ver != 7)
                {
                    throw LiteException.InvalidDatabase();
                }

                // skip ChangeID + FreeEmptyPageID + LastPageID
                reader.ReadBytes(2 + 4 + 4);
                page["userVersion"] = (int)reader.ReadUInt16();
                page["password"] = reader.ReadBytes(20);
                page["salt"] = reader.ReadBytes(16);
                page["collections"] = new BsonArray();

                var cols = reader.ReadByte();

                for (var i = 0; i < cols; i++)
                {
                    page["collections"].AsArray.Add(new BsonDocument
                    {
                        ["name"] = reader.ReadString(),
                        ["pageID"] = (int)reader.ReadUInt32()
                    });
                }
            }

            #endregion

            #region Collection (2)

            // collection page
            else if (page["pageType"] == 2)
            {
                page["collectionName"] = reader.ReadString();
                page["indexes"] = new BsonArray();
                reader.ReadBytes(12);

                for(var i = 0; i < 16; i++)
                {
                    var index = new BsonDocument();

                    var field = reader.ReadString();
                    var eq = field.IndexOf('=');

                    if (eq > 0)
                    {
                        index["name"] = field.Substring(0, eq);
                        index["expression"] = field.Substring(eq + 1);
                    }
                    else
                    {
                        index["name"] = field;
                        index["expression"] = "$." + field;
                    }

                    index["unique"] = reader.ReadBoolean();
                    index["headPageID"] = (int)reader.ReadUInt32();

                    // skip HeadNode (index) + TailNode + FreeIndexPageID
                    reader.ReadBytes(2 + 6 + 4);

                    if (field.Length > 0)
                    {
                        page["indexes"].AsArray.Add(index);
                    }
                }
            }

            #endregion

            #region Index (3)

            else if (page["pageType"] == 3)
            {
                page["nodes"] = new BsonArray();

                for(var i = 0; i < page["itemCount"].AsInt32; i++)
                {
                    var node = new BsonDocument
                    {
                        ["index"] = (int)reader.ReadUInt16()
                    };

                    var levels = reader.ReadByte();

                    // skip Slot + PrevNode + NextNode
                    reader.ReadBytes(1 + 6 + 6);

                    var length = reader.ReadUInt16();

                    // skip DataType + KeyValue
                    reader.ReadBytes(1 + length);

                    node["dataBlock"] = new BsonDocument
                    {
                        ["pageID"] = (int)reader.ReadUInt32(),
                        ["index"] = (int)reader.ReadUInt16()
                    };

                    // skip Prev[0]
                    reader.ReadBytes(6);

                    // reading Next[0]
                    node["next"] = new BsonDocument
                    {
                        ["pageID"] = (int)reader.ReadUInt32(),
                        ["index"] = (int)reader.ReadUInt16()
                    };

                    // skip Prev/Next[1..N]
                    reader.ReadBytes((levels - 1) * (6 + 6));

                    page["nodes"].AsArray.Add(node);
                }
            }

            #endregion

            #region Data (4)

            else if (page["pageType"] == 4)
            {
                page["blocks"] = new BsonArray();

                for (var i = 0; i < page["itemCount"].AsInt32; i++)
                {
                    var block = new BsonDocument
                    {
                        ["index"] = (int)reader.ReadUInt16(),
                        ["extendPageID"] = (int)reader.ReadUInt32()
                    };

                    var length = reader.ReadUInt16();

                    block["data"] = reader.ReadBytes(length);

                    page["blocks"].AsArray.Add(block);
                }
            }

            #endregion

            #region Extend (5)

            else if (page["pageType"] == 5)
            {
                page["data"] = reader.ReadBytes(page["itemCount"].AsInt32);
            }

            #endregion

            return page;
        }

        /// <summary>
        /// Read extend data block
        /// </summary>
        private byte[] ReadExtendData(uint extendPageID)
        {
            // read all extended pages and build byte array
            using (var buffer = new MemoryStream())
            {
                while(extendPageID != uint.MaxValue)
                {
                    var page = this.ReadPage(extendPageID);

                    buffer.Write(page["data"].AsBinary, 0, page["itemCount"].AsInt32);

                    extendPageID = (uint)page["nextPageID"].AsInt32;
                }

                return buffer.ToArray();
            }
        }

        public void Dispose()
        {
        }
    }
}