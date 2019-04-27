using LiteDB.Engine;
using System;
using System.IO;
using System.Text;

namespace LiteDB
{
    internal static class BinaryReaderExtensions
    {
        public static string ReadFixedString(this BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length);

            return Encoding.UTF8.GetString(bytes);
        }

        public static string ReadStringLegacy(this BinaryReader reader)
        {
            var length = reader.ReadInt32();
            return ReadFixedString(reader, length);
        }
    }
}