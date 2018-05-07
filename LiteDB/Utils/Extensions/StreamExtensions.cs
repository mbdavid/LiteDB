using System;
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
            var buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

#if HAVE_LOCK
        /// <summary>
        /// Try unlock stream segment. Do nothing if was not possible (it's not locked)
        /// </summary>
        public static bool TryUnlock(this FileStream stream, long position, long length)
        {
            if (length == 0) return true;

            try
            {
                stream.Unlock(position, length);

                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (PlatformNotSupportedException pex)
            {
                throw CreateLockNotSupportedException(pex);
            }
        }

        /// <summary>
        /// Try lock a stream segment during timeout.
        /// </summary>
        public static void TryLock(this FileStream stream, long position, long length, TimeSpan timeout)
        {
            if (length == 0) return;

            FileHelper.TryExec(() =>
            {
                try
                {
                    stream.Lock(position, length);
                }
                catch (PlatformNotSupportedException pex)
                {
                    throw CreateLockNotSupportedException(pex);
                }
            }, timeout);
        }

        private static Exception CreateLockNotSupportedException(PlatformNotSupportedException innerEx)
        {
            throw new InvalidOperationException("Your platform does not support FileStream.Lock. Please set mode=Exclusive in your connnection string to avoid this error.", innerEx);
        }
#endif
    }
}