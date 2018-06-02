using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Internal class to serialize a BsonDocument to BSON data format (byte[])
    /// </summary>
    public class BsonWriter
    {
        /// <summary>
        /// Main method - Serialize document into lazy ChunkStream
        /// </summary>
        public ChunkStream Serialize(BsonDocument doc)
        {
            var length = doc.GetBytesCount(true);

            //TODO: implement lazy serialization - do not use MemoryStream();
            // WriteDocument must return an IEnumerable

            var mem = new MemoryStream();
            var writer = new BinaryWriter(mem);

            this.WriteDocument(writer, doc);

            return new ChunkStream(new List<byte[]> { mem.ToArray() }, length);
        }

        /// <summary>
        /// Write a bson document into ByteWriter
        /// </summary>
        public void WriteDocument(BinaryWriter writer, BsonDocument doc)
        {
            writer.Write(doc.GetBytesCount(false));

            foreach (var key in doc.Keys)
            {
                this.WriteElement(writer, key, doc[key] ?? BsonValue.Null);
            }

            writer.Write((byte)0x00);
        }

        internal void WriteArray(BinaryWriter writer, BsonArray array)
        {
            writer.Write(array.GetBytesCount(false));

            for (var i = 0; i < array.Count; i++)
            {
                this.WriteElement(writer, i.ToString(), array[i] ?? BsonValue.Null);
            }

            writer.Write((byte)0x00);
        }

        private void WriteElement(BinaryWriter writer, string key, BsonValue value)
        {
            // cast RawValue to avoid one if on As<Type>
            switch (value.Type)
            {
                case BsonType.Double:
                    writer.Write((byte)0x01);
                    this.WriteCString(writer, key);
                    writer.Write((Double)value.RawValue);
                    break;

                case BsonType.String:
                    writer.Write((byte)0x02);
                    this.WriteCString(writer, key);
                    this.WriteString(writer, (String)value.RawValue);
                    break;

                case BsonType.Document:
                    writer.Write((byte)0x03);
                    this.WriteCString(writer, key);
                    this.WriteDocument(writer, new BsonDocument((Dictionary<string, BsonValue>)value.RawValue));
                    break;

                case BsonType.Array:
                    writer.Write((byte)0x04);
                    this.WriteCString(writer, key);
                    this.WriteArray(writer, new BsonArray((List<BsonValue>)value.RawValue));
                    break;

                case BsonType.Binary:
                    writer.Write((byte)0x05);
                    this.WriteCString(writer, key);
                    var bytes = (byte[])value.RawValue;
                    writer.Write(bytes.Length);
                    writer.Write((byte)0x00); // subtype 00 - Generic binary subtype
                    writer.Write(bytes);
                    break;

                case BsonType.Guid:
                    writer.Write((byte)0x05);
                    this.WriteCString(writer, key);
                    var guid = ((Guid)value.RawValue).ToByteArray();
                    writer.Write(guid.Length);
                    writer.Write((byte)0x04); // UUID
                    writer.Write(guid);
                    break;

                case BsonType.ObjectId:
                    writer.Write((byte)0x07);
                    this.WriteCString(writer, key);
                    writer.Write(((ObjectId)value.RawValue).ToByteArray());
                    break;

                case BsonType.Boolean:
                    writer.Write((byte)0x08);
                    this.WriteCString(writer, key);
                    writer.Write((byte)(((Boolean)value.RawValue) ? 0x01 : 0x00));
                    break;

                case BsonType.DateTime:
                    writer.Write((byte)0x09);
                    this.WriteCString(writer, key);
                    var date = (DateTime)value.RawValue;
                    // do not convert to UTC min/max date values - #19
                    var utc = (date == DateTime.MinValue || date == DateTime.MaxValue) ? date : date.ToUniversalTime();
                    var ts = utc - BsonValue.UnixEpoch;
                    writer.Write(Convert.ToInt64(ts.TotalMilliseconds));
                    break;

                case BsonType.Null:
                    writer.Write((byte)0x0A);
                    this.WriteCString(writer, key);
                    break;

                case BsonType.Int32:
                    writer.Write((byte)0x10);
                    this.WriteCString(writer, key);
                    writer.Write((Int32)value.RawValue);
                    break;

                case BsonType.Int64:
                    writer.Write((byte)0x12);
                    this.WriteCString(writer, key);
                    writer.Write((Int64)value.RawValue);
                    break;

                case BsonType.Decimal:
                    writer.Write((byte)0x13);
                    this.WriteCString(writer, key);
                    writer.Write((Decimal)value.RawValue);
                    break;

                case BsonType.MinValue:
                    writer.Write((byte)0xFF);
                    this.WriteCString(writer, key);
                    break;

                case BsonType.MaxValue:
                    writer.Write((byte)0x7F);
                    this.WriteCString(writer, key);
                    break;
            }
        }

        private void WriteString(BinaryWriter writer, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            writer.Write(bytes.Length + 1);
            writer.Write(bytes);
            writer.Write((byte)0x00);
        }

        private void WriteCString(BinaryWriter writer, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            writer.Write(bytes);
            writer.Write((byte)0x00);
        }
    }
}