using LiteDB.Engine;
using System;
using System.IO;
using static LiteDB.Constants;

namespace LiteDB
{
    internal static class StreamExtensions
    {
        /// <summary>
        /// Lock FileStream in fixed page 2. Support try/catch for some type
        /// </summary>
        public static bool TryLock(this Stream stream, TimeSpan timeout)
        {
            // not working
            throw new NotImplementedException();
            /*
            var filestream = stream as FileStream;

            if (filestream == null) return true;

            return FileHelper.TryExec(() =>
            {
                filestream.Lock(0, PAGE_HEADER_SIZE);
            },
            timeout);*/
        }

        public static void TryUnlock(this Stream stream)
        {
            var filestream = stream as FileStream;

            if (filestream == null) return;

            try
            {
                filestream.Unlock(0, PAGE_HEADER_SIZE);
            }
            catch
            {
            }
        }
    }
}