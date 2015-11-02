using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class BsonWriter
    {
        public void Serialize(Stream stream, BsonDocument value)
        {
            var writer = new BinaryWriter(stream);

            this.WriteDocument(writer, value);
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
                    var utc =  (date == DateTime.MinValue || date == DateTime.MaxValue) ? date : date.ToUniversalTime();
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

        /// <summary>
        /// Write a bson document
        /// </summary>
        internal void WriteDocument(BinaryWriter writer, BsonDocument doc)
        {
            using (var mem = new MemoryStream())
            {
                var w = new BinaryWriter(mem);

                foreach (var key in doc.Keys)
                {
                    this.WriteElement(w, key, doc[key] ?? BsonValue.Null);
                }

                writer.Write((Int32)mem.Position);
                writer.Write(mem.GetBuffer(), 0, (int)mem.Position);
                writer.Write((byte)0x00);
            }
        }

        internal void WriteArray(BinaryWriter writer, BsonArray arr)
        {
            using (var mem = new MemoryStream())
            {
                var w = new BinaryWriter(mem);

                for (var i = 0; i < arr.Count; i++)
                {
                    this.WriteElement(w, i.ToString(), arr[i] ?? BsonValue.Null);
                }

                writer.Write((Int32)mem.Position);
                writer.Write(mem.GetBuffer(), 0, (int)mem.Position);
                writer.Write((byte)0x00);
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
