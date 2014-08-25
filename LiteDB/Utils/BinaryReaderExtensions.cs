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

        public static IndexKey ReadIndexKey(this BinaryReader reader)
        {
            var type = (IndexDataType)reader.ReadByte();

            switch (type)
            {
                case IndexDataType.Null: return new IndexKey(null);

                // int
                case IndexDataType.Byte: return new IndexKey(reader.ReadByte());
                case IndexDataType.Int16: return new IndexKey(reader.ReadInt16());
                case IndexDataType.UInt16: return new IndexKey(reader.ReadUInt16());
                case IndexDataType.Int32: return new IndexKey(reader.ReadInt32());
                case IndexDataType.UInt32: return new IndexKey(reader.ReadUInt32());
                case IndexDataType.Int64: return new IndexKey(reader.ReadInt64());
                case IndexDataType.UInt64: return new IndexKey(reader.ReadUInt64());

                // decimal
                case IndexDataType.Single: return new IndexKey(reader.ReadSingle());
                case IndexDataType.Double: return new IndexKey(reader.ReadDouble());
                case IndexDataType.Decimal: return new IndexKey(reader.ReadDecimal());

                // string
                case IndexDataType.String:
                    var l = reader.ReadByte();
                    return new IndexKey(reader.ReadString(l));

                // others
                case IndexDataType.DateTime: return new IndexKey(reader.ReadDateTime());
                case IndexDataType.Guid: return new IndexKey(reader.ReadGuid());
            }

            throw new NotImplementedException();
        }

        public static long Seek(this BinaryReader reader, long position)
        {
            return reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }
}
