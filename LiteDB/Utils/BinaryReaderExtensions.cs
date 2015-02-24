using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LiteDB
{
    internal static class BinaryReaderExtensions
    {
        public static string ReadString(this BinaryReader reader, int size)
        {
            var bytes = reader.ReadBytes(size);
            string str = Encoding.UTF8.GetString(bytes);
            return str.Replace((char)0, ' ').Trim();
        }

        public static Guid ReadGuid(this BinaryReader reader)
        {
            return new Guid(reader.ReadBytes(16));
        }

        public static DateTime ReadDateTime(this BinaryReader reader)
        {
            return new DateTime(reader.ReadInt64());
        }

        public static PageAddress ReadPageAddress(this BinaryReader reader)
        {
            return new PageAddress(reader.ReadUInt32(), reader.ReadUInt16());
        }

        public static BsonValue ReadBsonValue(this BinaryReader reader)
        {
            var type = (BsonType)reader.ReadByte();

            switch (type)
            {
                // fixed length
                case BsonType.Null: return BsonValue.Null;

                case BsonType.Int32: return reader.ReadInt32();
                case BsonType.Int64: return reader.ReadInt64();
                case BsonType.Double: return reader.ReadDouble();

                case BsonType.Guid: return reader.ReadGuid();

                case BsonType.Boolean: return reader.ReadBoolean();
                case BsonType.DateTime: return reader.ReadDateTime();

                // variable lengths
                case BsonType.String:
                    var ls = reader.ReadByte();
                    return reader.ReadString(ls);

                case BsonType.Binary:
                    var lb = reader.ReadByte();
                    return reader.ReadBytes(lb);

                // for document/array uses BsonReader
                case BsonType.Document:
                    return new BsonReader().ReadDocument(reader);

                case BsonType.Array:
                    return new BsonReader().ReadArray(reader);
            }

            throw new NotImplementedException();
        }
    }
}
