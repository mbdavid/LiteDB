using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    internal class BsonReader
    {
        private BinaryReader _reader;

        public BsonReader(Stream stream)
        {
            _reader = new BinaryReader(stream);
        }

        public BsonDocument Deserialize()
        {
            return this.ReadDocument();
        }

        private BsonValue ReadElement(out string name)
        {
            var type = _reader.ReadByte();
            name = this.ReadCString();

            if (type == 0x01) // Double
            {
                return _reader.ReadDouble();
            }
            else if (type == 0x02) // String
            {
                return this.ReadString();
            }
            else if (type == 0x03) // Document
            {
                return this.ReadDocument();
            }
            else if (type == 0x04) // Array
            {
                return this.ReadArray();
            }
            else if (type == 0x05) // Binary
            {
                var length = _reader.ReadInt32();
                var subType = _reader.ReadByte();
                var bytes = _reader.ReadBytes(length);

                switch (subType)
                {
                    case 0x00: return bytes;
                    case 0x04: return new Guid(bytes);
                }
            }
            else if (type == 0x08) // Boolean
            {
                return _reader.ReadBoolean();
            }
            else if (type == 0x09) // DateTime
            {
                var ts = _reader.ReadInt64();

                return BsonValue.UnixEpoch.AddMilliseconds(ts).ToLocalTime();
            }
            else if (type == 0x0A) // Null
            {
                return BsonValue.Null;
            }
            else if (type == 0x10) // Int32
            {
                return _reader.ReadInt32();
            }
            else if (type == 0x12) // Int64
            {
                return _reader.ReadInt64();
            }

            throw new LiteException("Bson type not supported");
        }

        public BsonDocument ReadDocument()
        {
            var length = _reader.ReadInt32();
            var end = (int)_reader.BaseStream.Position + length - 1;
            var obj = new BsonDocument();

            while (_reader.BaseStream.Position < end)
            {
                string name;
                var value = this.ReadElement(out name);
                obj.RawValue[name] = value;
            }

            _reader.ReadByte(); // zero

            return obj;
        }

        public BsonArray ReadArray()
        {
            var length = _reader.ReadInt32();
            var end = (int)_reader.BaseStream.Position + length - 1;
            var arr = new BsonArray();

            while (_reader.BaseStream.Position < end)
            {
                string name;
                var value = this.ReadElement(out name);
                arr.Add(value);
            }

            _reader.ReadByte(); // zero

            return arr;
        }

        private string ReadString()
        {
            var length = _reader.ReadInt32();
            var bytes = _reader.ReadBytes(length - 1);
            _reader.ReadByte(); // discard \x00
            return Encoding.UTF8.GetString(bytes);
        }

        private string ReadCString()
        {
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    byte buf = _reader.ReadByte();
                    if (buf == 0x00) break;
                    ms.WriteByte(buf);
                }

                return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
            }
        }
    }
}
