using System;
using System.IO;
using System.Text;

namespace LiteDB
{
    internal static class BinaryReaderExtensions
    {
        public static Guid ReadGuid(this BinaryReader reader)
        {
            return new Guid(reader.ReadBytes(16));
        }

        public static DateTime ReadDateTime(this BinaryReader reader, bool utcDate)
        {
            var utc = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);

            return utcDate ? utc : utc.ToLocalTime();
        }

        public static ObjectId ReadObjectId(this BinaryReader reader)
        {
            return new ObjectId(reader.ReadBytes(12));
        }

        public static PageAddress ReadPageAddress(this BinaryReader reader)
        {
            return new PageAddress(reader.ReadUInt32(), reader.ReadUInt16());
        }

        public static BsonDocument ReadDocument(this BinaryReader reader, bool utcDate)
        {
            var bson = new BsonReader(utcDate);
            return bson.ReadDocument(reader, null);
        }

        public static BsonArray ReadArray(this BinaryReader reader, bool utcDate)
        {
            var bson = new BsonReader(utcDate);
            return bson.ReadArray(reader);
        }

        /// <summary>
        /// Read BSON value from reader
        /// </summary>
        public static BsonValue ReadBsonValue(this BinaryReader reader, bool utcDate)
        {
            var type = (BsonType)reader.ReadByte();

            switch (type)
            {
                case BsonType.Null: return BsonValue.Null;

                case BsonType.Int32: return reader.ReadInt32();
                case BsonType.Int64: return reader.ReadInt64();
                case BsonType.Double: return reader.ReadDouble();
                case BsonType.Decimal: return reader.ReadDecimal();

                case BsonType.String: return reader.ReadString();

                case BsonType.Document: return reader.ReadDocument(utcDate);
                case BsonType.Array: return reader.ReadArray(utcDate);

                case BsonType.Binary:
                    var length = reader.ReadUInt16();
                    return reader.ReadBytes(length);
                case BsonType.ObjectId: return reader.ReadObjectId();
                case BsonType.Guid: return reader.ReadGuid();

                case BsonType.Boolean: return reader.ReadBoolean();
                case BsonType.DateTime: return reader.ReadDateTime(utcDate);

                case BsonType.MinValue: return BsonValue.MinValue;
                case BsonType.MaxValue: return BsonValue.MaxValue;
            }

            throw new NotImplementedException();
        }
    }
}