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
            var bytes = Encoding.UTF8.GetBytes(text);

            if (bytes.Length != length)
            {
                throw new ArgumentException("Invalid string length");
            }

            writer.Write(bytes);
        }

        public static void Write(this BinaryWriter writer, ObjectId oid)
        {
            writer.Write(oid.ToByteArray());
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

        public static void WriteBsonValue(this BinaryWriter writer, BsonValue value, ushort length)
        {
            writer.Write((byte)value.Type);

            switch(value.Type)
            {
                case BsonType.Null:
                case BsonType.MinValue:
                case BsonType.MaxValue:
                    break;

                case BsonType.Int32: writer.Write((Int32)value.RawValue); break;
                case BsonType.Int64: writer.Write((Int64)value.RawValue); break;
                case BsonType.Double: writer.Write((Double)value.RawValue); break;

                case BsonType.String: writer.Write((String)value.RawValue, length); break;

                case BsonType.Document: new BsonWriter().WriteDocument(writer, value.AsDocument); break;
                case BsonType.Array: new BsonWriter().WriteArray(writer, value.AsArray); break;

                case BsonType.Binary: writer.Write((Byte[])value.RawValue); break;
                case BsonType.ObjectId: writer.Write((ObjectId)value.RawValue); break;
                case BsonType.Guid: writer.Write((Guid)value.RawValue); break;

                case BsonType.Boolean: writer.Write((Boolean)value.RawValue); break;
                case BsonType.DateTime: writer.Write((DateTime)value.RawValue); break;

                default: throw new NotImplementedException();
            }
        }
    }
}
