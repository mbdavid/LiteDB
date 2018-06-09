using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using LiteDB.Engine;

namespace LiteDB
{
    internal static class BinaryWriterExtensions
    {
        /// <summary>
        /// Write string into writer without length information (must know when read)
        /// </summary>
        public static void WriteFixedString(this BinaryWriter writer, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);

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

        public static void WriteDocument(this BinaryWriter writer, BsonDocument doc)
        {
            var bson = new BsonWriter();
            bson.WriteDocument(writer, doc);
        }

        public static void WriteArray(this BinaryWriter writer, BsonArray array)
        {
            var bson = new BsonWriter();
            bson.WriteArray(writer, array);
        }

        public static void WriteBsonValue(this BinaryWriter writer, BsonValue value)
        {
            writer.Write((byte)value.Type);

            switch (value.Type)
            {
                case BsonType.Null:
                case BsonType.MinValue:
                case BsonType.MaxValue:
                    break;

                case BsonType.Int32: writer.Write((Int32)value.RawValue); break;
                case BsonType.Int64: writer.Write((Int64)value.RawValue); break;
                case BsonType.Double: writer.Write((Double)value.RawValue); break;
                case BsonType.Decimal: writer.Write((Decimal)value.RawValue); break;

                case BsonType.String: writer.WriteFixedString((String)value.RawValue); break;

                case BsonType.Document: writer.WriteDocument(value.AsDocument); break;
                case BsonType.Array: writer.WriteArray(value.AsArray); break;

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