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

        public static void Write(this BinaryWriter writer, IndexKey obj)
        {
            writer.Write((byte)obj.Type);

            switch(obj.Type)
            {
                case IndexDataType.Int32: writer.Write((Int32)obj.Value); break;
                case IndexDataType.Int64: writer.Write((Int64)obj.Value); break;
                case IndexDataType.Double: writer.Write((Double)obj.Value); break;
                case IndexDataType.String:
                    var length = (byte)Encoding.UTF8.GetByteCount((String)obj.Value);
                    writer.Write(length);
                    writer.Write((String)obj.Value, length);
                    break;
                case IndexDataType.Boolean: writer.Write((Boolean)obj.Value); break;
                case IndexDataType.DateTime: writer.Write((DateTime)obj.Value); break;
                case IndexDataType.Guid: writer.Write((Guid)obj.Value); break;
            }

            // otherwise is null - write only obj.Type = Null
        }
    }
}
