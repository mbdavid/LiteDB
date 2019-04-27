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
        private BinaryReader _reader;

        public DateTime CreationTime { get; set; } = DateTime.Now;
        public int UserVersion { get; set; }

        public FileReaderV7(Stream stream)
        {
            _reader = new BinaryReader(stream);

            // only userVersion was avaiable in old file format versions
            this.UserVersion = this.ReadPage(0)["userVersion"].AsInt32;
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
            // v7 use 4K page size
            _reader.BaseStream.Position = pageID * 4096;

            // reading page header
            var page = new BsonDocument
            {
                ["pageID"] = (int)_reader.ReadUInt32(),
                ["pageType"] = (int)_reader.ReadByte(),
                ["prevPageID"] = (int)_reader.ReadUInt32(),
                ["nextPageID"] = (int)_reader.ReadUInt32(),
                ["itemCount"] = (int)_reader.ReadUInt16()
            };

            // skip freeByte + reserved
            _reader.ReadBytes(2 + 8);

            #region Header

            // read header
            if (page["pageType"] == 1)
            {
                // skip HEADER_INFO + VERSION + ChangeID + FreeEmptyPageID + LastPageID
                _reader.ReadBytes(27 + 1 + 2 + 4 + 4);
                page["userVersion"] = (int)_reader.ReadUInt16();
                page["password"] = _reader.ReadBytes(20);
                page["salt"] = _reader.ReadBytes(16);
                page["collections"] = new BsonArray();

                var cols = _reader.ReadByte();

                for (var i = 0; i < cols; i++)
                {
                    page["collections"].AsArray.Add(new BsonDocument
                    {
                        ["name"] = _reader.ReadStringLegacy(),
                        ["pageID"] = (int)_reader.ReadUInt32()
                    });
                }
            }

            #endregion

            #region Collection

            // collection page
            else if (page["pageType"] == 2)
            {
                page["collectionName"] = _reader.ReadStringLegacy();
                page["indexes"] = new BsonArray();
                _reader.ReadBytes(12);

                for(var i = 0; i < 16; i++)
                {
                    var index = new BsonDocument();

                    var field = _reader.ReadStringLegacy();
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

                    index["unique"] = _reader.ReadBoolean();
                    index["headPageID"] = (int)_reader.ReadUInt32();

                    // skip HeadNode (index) + TailNode + FreeIndexPageID
                    _reader.ReadBytes(2 + 6 + 4);

                    if (field.Length > 0)
                    {
                        page["indexes"].AsArray.Add(index);
                    }
                }
            }

            #endregion

            #region Index

            else if (page["pageType"] == 3)
            {
                page["nodes"] = new BsonArray();

                for(var i = 0; i < page["itemCount"].AsInt32; i++)
                {
                    var node = new BsonDocument
                    {
                        ["index"] = (int)_reader.ReadUInt16()
                    };

                    var levels = _reader.ReadByte();

                    // skip Slot + PrevNode + NextNode
                    _reader.ReadBytes(1 + 6 + 6);

                    var length = _reader.ReadUInt16();

                    // skip DataType + KeyValue
                    _reader.ReadBytes(1 + length);

                    node["dataBlock"] = new BsonDocument
                    {
                        ["pageID"] = (int)_reader.ReadUInt32(),
                        ["index"] = (int)_reader.ReadUInt16()
                    };

                    // skip Prev[0]
                    _reader.ReadBytes(6);

                    // reading Next[0]
                    node["next"] = new BsonDocument
                    {
                        ["pageID"] = (int)_reader.ReadUInt32(),
                        ["index"] = (int)_reader.ReadUInt16()
                    };

                    // skip Prev/Next[1..N]
                    _reader.ReadBytes((levels - 1) * (6 + 6));

                    page["nodes"].AsArray.Add(node);
                }
            }

            #endregion

            #region Data

            else if (page["pageType"] == 4)
            {
                page["blocks"] = new BsonArray();

                for (var i = 0; i < page["itemCount"].AsInt32; i++)
                {
                    var block = new BsonDocument
                    {
                        ["index"] = (int)_reader.ReadUInt16(),
                        ["extendPageID"] = (int)_reader.ReadUInt32()
                    };

                    var length = _reader.ReadUInt16();

                    block["data"] = _reader.ReadBytes(length);

                    page["blocks"].AsArray.Add(block);
                }
            }

            #endregion

            #region Extend

            else if (page["pageType"] == 5)
            {
                page["data"] = _reader.ReadBytes(page["itemCount"].AsInt32);
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
    }
}