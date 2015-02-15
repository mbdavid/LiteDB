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
        internal static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private Stream _stream;

        public BsonWriter(Stream stream)
        {
            _stream = stream;
        }

        public void Serialize(BsonObject value)
        {
            var writer = new BinaryWriter(_stream);

            this.WriteDocument(writer, value);
        }

        private void WriteElement(BinaryWriter writer, string key, BsonValue value)
        {
            if (value.IsDouble)
            {
                writer.Write((byte)0x01);
                this.WriteCString(writer, key);
                writer.Write(value.AsDouble);
            }
            else if (value.IsString)
            {
                writer.Write((byte)0x02);
                this.WriteCString(writer, key);
                this.WriteString(writer, value.AsString);
            }
            else if (value.IsObject)
            {
                writer.Write((byte)0x03);
                this.WriteCString(writer, key);
                this.WriteDocument(writer, value.AsObject);
            }
            else if (value.IsArray)
            {
                writer.Write((byte)0x04);
                this.WriteCString(writer, key);
                this.WriteArray(writer, value.AsArray);
            }
            else if (value.IsBinary)
            {
                writer.Write((byte)0x05);
                this.WriteCString(writer, key);
                var bytes = value.AsBinary;
                writer.Write(bytes.Length);
                writer.Write((byte)0x00); // subtype 00 - Generic binary subtype
                writer.Write(bytes);
            }
            else if (value.IsGuid)
            {
                writer.Write((byte)0x05);
                this.WriteCString(writer, key);
                var bytes = value.AsGuid.ToByteArray();
                writer.Write(bytes.Length);
                writer.Write((byte)0x04); // UUID
                writer.Write(bytes);
            }
            else if (value.IsBoolean)
            {
                writer.Write((byte)0x08);
                this.WriteCString(writer, key);
                writer.Write((byte)(value.AsBoolean ? 0x01 : 0x00));
            }
            else if (value.IsDateTime)
            {
                writer.Write((byte)0x09);
                this.WriteCString(writer, key);
                var utc = value.AsDateTime.ToUniversalTime();
                var ts = utc - UnixEpoch;
                writer.Write(Convert.ToInt64(ts.TotalMilliseconds));
            }
            else if (value.IsNull)
            {
                writer.Write((byte)0x0A);
                this.WriteCString(writer, key);
            }
            else if (value.IsInt32)
            {
                writer.Write((byte)0x10);
                this.WriteCString(writer, key);
                writer.Write(value.AsInt32);
            }
            else if (value.IsInt64)
            {
                writer.Write((byte)0x12);
                this.WriteCString(writer, key);
                writer.Write(value.AsInt64);
            }
        }

        /// <summary>
        /// Write a bson document
        /// </summary>
        private void WriteDocument(BinaryWriter writer, BsonObject obj)
        {
            using (var mem = new MemoryStream())
            {
                var w = new BinaryWriter(mem);

                foreach (var key in obj.Keys)
                {
                    this.WriteElement(w, key, obj[key]);
                }

                writer.Write((Int32)mem.Position);
                writer.Write(mem.GetBuffer(), 0, (int)mem.Position);
                writer.Write((byte)0x00);
            }
        }

        private void WriteArray(BinaryWriter writer, BsonArray arr)
        {
            using (var mem = new MemoryStream())
            {
                var w = new BinaryWriter(mem);

                for (var i = 0; i < arr.Count; i++)
                {
                    this.WriteElement(w, i.ToString(), arr[i]);
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
