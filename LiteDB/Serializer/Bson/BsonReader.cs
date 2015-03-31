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
        public BsonDocument Deserialize(Stream stream)
        {
            var reader = new BinaryReader(stream);

            return this.ReadDocument(reader);
        }

        private BsonValue ReadElement(BinaryReader reader, out string name)
        {
            var type = reader.ReadByte();
            name = this.ReadCString(reader);

            if (type == 0x01) // Double
            {
                return reader.ReadDouble();
            }
            else if (type == 0x02) // String
            {
                return this.ReadString(reader);
            }
            else if (type == 0x03) // Document
            {
                return this.ReadDocument(reader);
            }
            else if (type == 0x04) // Array
            {
                return this.ReadArray(reader);
            }
            else if (type == 0x05) // Binary
            {
                var length = reader.ReadInt32();
                var subType = reader.ReadByte();
                var bytes = reader.ReadBytes(length);

                switch (subType)
                {
                    case 0x00: return bytes;
                    case 0x04: return new Guid(bytes);
                }
            }
            else if (type == 0x07) // ObjectId
            {
                return new ObjectId(reader.ReadBytes(12));
            }
            else if (type == 0x08) // Boolean
            {
                return reader.ReadBoolean();
            }
            else if (type == 0x09) // DateTime
            {
                var ts = reader.ReadInt64();

                // catch specific values for MaxValue / MinValue #19
                if (ts == 253402300800000) return DateTime.MaxValue;
                if (ts == -62135596800000) return DateTime.MinValue;

                return BsonValue.UnixEpoch.AddMilliseconds(ts).ToLocalTime();
            }
            else if (type == 0x0A) // Null
            {
                return BsonValue.Null;
            }
            else if (type == 0x10) // Int32
            {
                return reader.ReadInt32();
            }
            else if (type == 0x12) // Int64
            {
                return reader.ReadInt64();
            }
            else if (type == 0xFF) // MinKey
            {
                return BsonValue.MinValue;
            }
            else if (type == 0x7F) // MaxKey
            {
                return BsonValue.MaxValue;
            }

            throw new NotSupportedException("BSON type not supported");
        }

        public BsonDocument ReadDocument(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var end = (int)reader.BaseStream.Position + length - 1;
            var obj = new BsonDocument();

            while (reader.BaseStream.Position < end)
            {
                string name;
                var value = this.ReadElement(reader, out name);
                obj.RawValue[name] = value;
            }

            reader.ReadByte(); // zero

            return obj;
        }

        public BsonArray ReadArray(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var end = (int)reader.BaseStream.Position + length - 1;
            var arr = new BsonArray();

            while (reader.BaseStream.Position < end)
            {
                string name;
                var value = this.ReadElement(reader, out name);
                arr.Add(value);
            }

            reader.ReadByte(); // zero

            return arr;
        }

        private string ReadString(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var bytes = reader.ReadBytes(length - 1);
            reader.ReadByte(); // discard \x00
            return Encoding.UTF8.GetString(bytes);
        }

        private string ReadCString(BinaryReader reader)
        {
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    byte buf = reader.ReadByte();
                    if (buf == 0x00) break;
                    ms.WriteByte(buf);
                }

                return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
            }
        }
    }
}
