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
            var position = BasePage.GetSizeOfPages(pageID);

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
            var position = BasePage.GetSizeOfPages(pageID);

            // this lock is only for precaution
            lock (stream)
            {
                // position cursor
                stream.Position = position;

                stream.Write(buffer, 0, BasePage.PAGE_SIZE);

                return stream.Position;
            }
        }
    }
}