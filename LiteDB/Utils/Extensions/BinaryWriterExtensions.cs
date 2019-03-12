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
            writer.Write(dateTime.ToUniversalTime().Ticks);
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

        public static void WriteList(this BinaryWriter writer, BsonArray array)
        {
            var bson = new BsonWriter();
            bson.WriteList(writer, array);
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

                case BsonType.Int32: writer.Write(value.Int32Value); break;
                case BsonType.Int64: writer.Write(value.Int64Value); break;
                case BsonType.Double: writer.Write(value.DoubleValue); break;
                case BsonType.Decimal: writer.Write(value.DecimalValue); break;

                case BsonType.String: writer.WriteFixedString(value.StringValue); break;

                case BsonType.Document: writer.WriteDocument(value.AsDocument); break;

                case BsonType.List: 
                case BsonType.Array: writer.WriteList(value.AsArray); break;

                case BsonType.Binary: writer.Write(value.BinaryValue); break;
                case BsonType.ObjectId: writer.Write(value.ObjectIdValue); break;
                case BsonType.Guid: writer.Write(value.GuidValue); break;

                case BsonType.Boolean: writer.Write(value.BoolValue); break;
                case BsonType.DateTime: writer.Write(value.DateTimeValue); break;

                default: throw new NotImplementedException();
            }
        }
    }
}