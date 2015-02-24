using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace LiteDB
{
    internal static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter writer, string text, int length)
        {
            if (string.IsNullOrEmpty(text))
            {
                writer.Write(new byte[length]);
                return;
            }

            var buffer = new byte[length];
            var strbytes = Encoding.UTF8.GetBytes(text);

            Array.Copy(strbytes, buffer, length > strbytes.Length ? strbytes.Length : length);

            writer.Write(buffer);
        }

        public static void Write(this BinaryWriter writer, Guid guid)
        {
            writer.Write(guid.ToByteArray());
        }

        public static void Write(this BinaryWriter writer, DateTime dateTime)
        {
            writer.Write(dateTime.Ticks);
        }

        public static void Write(this BinaryWriter writer, PageAddress address)
        {
            writer.Write(address.PageID);
            writer.Write(address.Index);
        }

        public static void WriteBsonValue(this BinaryWriter writer, BsonValue value)
        {
            writer.Write((byte)value.Type);

            switch(value.Type)
            {
                // fixed length
                case BsonType.Null: break;

                case BsonType.Int32: writer.Write((Int32)value.RawValue); break;
                case BsonType.Int64: writer.Write((Int64)value.RawValue); break;
                case BsonType.Double: writer.Write((Double)value.RawValue); break;

                case BsonType.Guid: writer.Write((Guid)value.RawValue); break;
                case BsonType.Boolean: writer.Write((Boolean)value.RawValue); break;
                case BsonType.DateTime: writer.Write((DateTime)value.RawValue); break;

                // variable lengths
                case BsonType.String:
                    var str = (String)value.RawValue;
                    var length = (byte)Encoding.UTF8.GetByteCount(str);
                    writer.Write(length); // 1 byte for length
                    writer.Write(str, length);
                    break;

                case BsonType.Binary:
                    var bytes = (Byte[])value.RawValue;
                    writer.Write((byte)bytes.Length); // 1 byte for length
                    writer.Write(bytes);
                    break;

                // for document and array used BsonWriter
                case BsonType.Document:
                    new BsonWriter().WriteDocument(writer, value.AsDocument);
                    break;

                case BsonType.Array:
                    new BsonWriter().WriteArray(writer, value.AsArray);
                    break;

                default: throw new NotImplementedException();
            }
        }
    }
}
