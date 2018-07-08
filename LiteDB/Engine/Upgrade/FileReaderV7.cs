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
    internal class FileReaderV7
    {
        private const int PAGE_SIZE = 4096;

        private BinaryReader _reader;

        public FileReaderV7(Stream stream)
        {
            _reader = new BinaryReader(stream);
        }

        public string[] GetCollections()
        {
            var header = this.ReadPage(0);
            var names = header["collections"].AsArray.Select(x => x["name"].AsString).ToArray();

            return names;
        }

        private BsonDocument ReadPage(uint pageID)
        {
            _reader.BaseStream.Position = pageID * PAGE_SIZE;

            var page = new BsonDocument
            {
                ["pageID"] = (int)_reader.ReadUInt32(),
                ["pageType"] = (int)_reader.ReadByte(),
                ["prevPageID"] = (int)_reader.ReadUInt32(),
                ["nextPageID"] = (int)_reader.ReadUInt32(),
                ["itemCount"] = (int)_reader.ReadUInt16()
            };

            _reader.ReadBytes(2 + 8);

            // read header
            if (page["pageType"] == 1)
            {
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

            return page;
        }

    }
}