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

        public static void CopyTo(this Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024]; // Fairly arbitrary size
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }
    }
}