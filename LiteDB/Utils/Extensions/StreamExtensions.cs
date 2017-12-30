using System;
using System.IO;

namespace LiteDB
{
    internal static class StreamExtensions
    {
        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public static byte[] ReadPage(this Stream stream, uint pageID)
        {
            var buffer = new byte[BasePage.PAGE_SIZE];
            var position = BasePage.GetPagePostion(pageID);

            // this lock is only for precaution
            lock (stream)
            {
                // position cursor
                stream.Position = position;

                // read bytes from data file
                stream.Read(buffer, 0, BasePage.PAGE_SIZE);
            }

            return buffer;
        }

        /// <summary>
        /// Persist single page bytes to disk
        /// </summary>
        public static long WritePage(this Stream stream, uint pageID, byte[] buffer)
        {
            var position = BasePage.GetPagePostion(pageID);

            // this lock is only for precaution
            lock (stream)
            {
                // position cursor
                stream.Position = position;

                stream.Write(buffer, 0, BasePage.PAGE_SIZE);

                return stream.Position;
            }
        }

        /// <summary>
        /// Create new database based if Stream are empty
        /// </summary>
        public static void CreateDatabase(this Stream stream, long initialSize)
        {
            // create database only if not exists
            if (stream.Length == 0) return;

            // create a new header page in bytes (keep second page empty)
            var header = new HeaderPage
            {
                LastPageID = 1,
                Salt = AesEncryption.Salt()
            };

            // point to begin file
            stream.Seek(0, SeekOrigin.Begin);

            // get header page in bytes
            var buffer = header.WritePage();

            stream.Write(buffer, 0, BasePage.PAGE_SIZE);

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                stream.SetLength(initialSize);
            }
        }
    }
}