using System;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Internal class to deserialize a byte[] into a BsonDocument using BSON data format
    /// </summary>
    internal struct BsonReader
    {
        private bool _utcDate;

        public BsonReader(bool utcDate)
        {
            _utcDate = utcDate;
        }

        /// <summary>
        /// Main method - deserialize using ByteReader helper
        /// </summary>
        public BsonDocument Deserialize(byte[] bson)
        {
            var reader = new ByteReader(bson);
            return this.ReadDocument(ref reader);
        }

        /// <summary>
        /// Read a BsonDocument from reader
        /// </summary>
        public BsonDocument ReadDocument(ref ByteReader reader)
        {
            var length = reader.ReadInt32();
            var end = reader.Position + length - 5;
            var obj = new BsonDocument();

            while (reader.Position < end)
            {
                var value = this.ReadElement(ref reader, out string name);
                obj.RawValue[name] = value;
            }

            reader.Read0x00();

            return obj;
        }

        /// <summary>
        /// Read an BsonArray from reader
        /// </summary>
        public BsonArray ReadArray(ref ByteReader reader)
        {
            var length = reader.ReadInt32();
            var end = reader.Position + length - 5;
            var arr = new BsonArray();

            while (reader.Position < end)
            {
                var value = this.ReadElement(ref reader, out string name);
                if (StringToInt(name) != arr.Count) {
                    throw new Exception($"wrong array element name. expected \"{arr.Count}\", got \"{name}\"");
                }
                arr.Add(value);
            }

            reader.Read0x00();

            return arr;
        }

        /// <summary>
        /// Reads an element (key-value) from an reader
        /// </summary>
        private BsonValue ReadElement(ref ByteReader reader, out string name)
        {
            var type = reader.ReadByte();
            name = reader.ReadCString();

            if (type == 0x01) // Double
            {
                return reader.ReadDouble();
            }
            else if (type == 0x02) // String
            {
                return reader.ReadBsonString();
            }
            else if (type == 0x03) // Document
            {
                return this.ReadDocument(ref reader);
            }
            else if (type == 0x04) // Array
            {
                return this.ReadArray(ref reader);
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
                return reader.ReadObjectId();
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

                var date = BsonValue.UnixEpoch.AddMilliseconds(ts);

                return _utcDate ? date : date.ToLocalTime();
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

        // Int32.Parse() does way too much than this simple implementation
        // since the former supports many number sytles.
        static int StringToInt(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            if (str.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(str));

            int result = 0;
            int digitSize = 1;
            for (int i = str.Length - 1; i >= 0; i--) {
                var ch = str[i];
                var digit = ch - '0';
                if (digit < 0 || digit > 9) {
                    if (ch == '-' && i == str.Length - 1) {
                        return -result;
                    }
                    throw new ArgumentException("unexpected char during parsing number string");
                }
                result += digitSize * digit;
                digitSize *= 10;
            }
            return result;
        }
    }
}