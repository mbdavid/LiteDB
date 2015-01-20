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

            // int
            if (obj.Type == IndexDataType.Byte) writer.Write((Byte)obj.Value);
            else if (obj.Type == IndexDataType.Int16) writer.Write((Int16)obj.Value);
            else if (obj.Type == IndexDataType.UInt16) writer.Write((UInt16)obj.Value);
            else if (obj.Type == IndexDataType.Int32) writer.Write((Int32)obj.Value);
            else if (obj.Type == IndexDataType.UInt32) writer.Write((UInt32)obj.Value);
            else if (obj.Type == IndexDataType.Int64) writer.Write((Int64)obj.Value);
            else if (obj.Type == IndexDataType.UInt64) writer.Write((UInt64)obj.Value);

            // decimal
            else if (obj.Type == IndexDataType.Single) writer.Write((Single)obj.Value);
            else if (obj.Type == IndexDataType.Double) writer.Write((Double)obj.Value);
            else if (obj.Type == IndexDataType.Decimal) writer.Write((Decimal)obj.Value);

            // string
            else if (obj.Type == IndexDataType.String)
            {
                var length = (byte)Encoding.UTF8.GetByteCount((String)obj.Value);
                writer.Write(length);
                writer.Write((String)obj.Value, length);
            }

            // other
            else if (obj.Type == IndexDataType.Boolean) writer.Write((Boolean)obj.Value);
            else if (obj.Type == IndexDataType.DateTime) writer.Write((DateTime)obj.Value);
            else if (obj.Type == IndexDataType.Guid) writer.Write((Guid)obj.Value);

            // otherwise is null
        }

        public static long Seek(this BinaryWriter writer, long position)
        {
            return writer.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }
}
