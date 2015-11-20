using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LiteDB
{
    internal static class StreamExtensions
    {
        public static byte ReadByte(this Stream stream, long position)
        {
            var buffer = new byte[1];
            stream.Seek(position, SeekOrigin.Begin);
            stream.Read(buffer, 0, 1);
            return buffer[0];
        }

        public static void WriteByte(this Stream stream, long position, byte value)
        {
            stream.Seek(position, SeekOrigin.Begin);
            stream.Write(new byte[] { value }, 0, 1);
        }
    }
}
