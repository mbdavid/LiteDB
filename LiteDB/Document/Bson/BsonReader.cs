using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Internal class to deserialize a ChunckStream into a BsonDocument using BSON data format
    /// </summary>
    internal class BsonReader
    {
        private readonly bool _utcDate;

        public BsonReader(bool utcDate)
        {
            _utcDate = utcDate;
        }

        public BsonDocument Deserialize(Stream stream, HashSet<string> fields)
        {
            using(var reader = new BinaryReader(stream))
            {
                return this.ReadDocument(reader, fields == null ? null : new HashSet<string>(fields));
            }
        }

        /// <summary>
        /// Read a BsonDocument from reader - support select fields ONLY in root level
        /// </summary>
        public BsonDocument ReadDocument(BinaryReader reader, HashSet<string> fields = null)
        {
            var length = reader.ReadInt32();
            var end = reader.BaseStream.Position + length - 5;
            var remaining = fields == null || fields.Contains("$") ? null : new HashSet<string>(fields);

            var doc = new BsonDocument();

            while (reader.BaseStream.Position < end && (remaining == null || remaining?.Count > 0))
            {
                var value = this.ReadElement(reader, remaining, out string name);

                // null value means are not selected field
                if (value != null)
                {
                    doc.RawValue[name] = value;

                    // remove from remaining fields
                    remaining?.Remove(name);
                }
            }

            reader.ReadByte(); // zero

            return doc;
        }

        /// <summary>
        /// Read an BsonArray from reader
        /// </summary>
        public BsonArray ReadArray(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var end = reader.BaseStream.Position + length - 5;
            var arr = new BsonArray();

            while (reader.BaseStream.Position < end)
            {
                var value = this.ReadElement(reader, null, out string name);
                arr.Add(value);
            }

            reader.ReadByte(); // zero

            return arr;
        }

        /// <summary>
        /// Reads an element (key-value) from an reader. If remaining != null and name not are in 
        /// </summary>
        private BsonValue ReadElement(BinaryReader reader, HashSet<string> remaining, out string name)
        {
            var type = reader.ReadByte();
            name = this.ReadCString(reader);

            // check if need skip this element
            if (remaining != null && !remaining.Contains(name))
            {
                // define skip length according type
                var length =
                    (type == 0x0A || type == 0xFF || type == 0x7F) ? 0 : // Null, MinValue, MaxValue
                    (type == 0x08) ? 1 : // Boolean
                    (type == 0x10) ? 4 : // Int
                    (type == 0x01 || type == 0x12 || type == 0x09) ? 8 : // Double, Int64, DateTime
                    (type == 0x07) ? 12 : // ObjectId
                    (type == 0x13) ? 16 : // Decimal
                    (type == 0x02) ? reader.ReadInt32() : // String
                    (type == 0x05) ? reader.ReadInt32() + 1 : // Binary (+1 for subtype)
                    (type == 0x03 || type == 0x04) ? reader.ReadInt32() - 4 : 0; // Document, Array (-4 to Length + zero)

                if (length > 0)
                {
                    reader.BaseStream.Seek(length, SeekOrigin.Current);
                }

                return null;
            }

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

                var utc = BsonValue.UnixEpoch.AddMilliseconds(ts);

                return _utcDate ? utc : utc.ToLocalTime();
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
            else if (type == 0x13) // Decimal
            {
                return reader.ReadDecimal();
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

        private string ReadString(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var bytes = reader.ReadBytes(length - 1);
            reader.ReadByte(); // discard \x00
            return Encoding.UTF8.GetString(bytes, 0, length - 1);
        }

        private string ReadCString(BinaryReader reader)
        {
            var pos = 0;
            var buffer = new byte[200];

            while (true)
            {
                var data = reader.ReadByte();
                if (data == 0x00 || pos == 200) break;
                buffer[pos++] = data;
            }

            return Encoding.UTF8.GetString(buffer, 0, pos);
        }
    }
}