using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB
{
    /// <summary>
    /// A simple file helper tool with static methods
    /// </summary>
    internal static class FileHelper
    {
        /// <summary>
        /// Create a temp filename based on original filename - checks if file exists (if exists, append counter number)
        /// </summary>
        public static string GetSufixFile(string filename, string suffix = "-temp", bool checkIfExists = true)
        {
            var count = 0;
            var temp = Path.Combine(Path.GetDirectoryName(filename), 
                Path.GetFileNameWithoutExtension(filename) + suffix + 
                Path.GetExtension(filename));

            while(checkIfExists && File.Exists(temp))
            {
                temp = Path.Combine(Path.GetDirectoryName(filename),
                    Path.GetFileNameWithoutExtension(filename) + suffix +
                    "-" + (++count) +
                    Path.GetExtension(filename));
            }

            return temp;
        }

        /// <summary>
        /// Try set length to a Stream - keep tring if get any error (during 1 minute)
        /// </summary>
        public static void TrySetLength(Stream stream, long length)
        {
            var sw = Stopwatch.StartNew();

            while(sw.ElapsedMilliseconds < 60000)
            {
                try
                {
                    stream.SetLength(length);

                    break;
                }
                catch
                {
                    Task.Delay(100).Wait();
                }
            }
        }

        /// <summary>
        /// Try lock file during timeout. Returns false if not possible
        /// </summary>
        public static bool TryLock(FileStream stream, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed < timeout)
            {
                try
                {
                    stream.Lock(0, stream.Length);

                    return true;
                }
                catch (IOException ex)
                {
                    ex.WaitIfLocked(100);
                }
            }

            return false;
        }

        /// <summary>
        /// Try unlock file during timeout. Returns false if not possible
        /// </summary>
        public static bool TryUnlock(FileStream stream)
        {
            try
            {
                stream.Unlock(0, stream.Length);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Try lock delete file - returns true if file exists and are not opened by another process
        /// </summary>
        public static bool TryDelete(string filename)
        {
            try
            {
                File.Delete(filename);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convert storage unit string "1gb", "10 mb", "80000" to long bytes
        /// </summary>
        public static long ParseFileSize(string size)
        {
            var match = Regex.Match(size, @"^(\d+)\s*([tgmk])?(b|byte|bytes)?$", RegexOptions.IgnoreCase);

            if (!match.Success) return 0;

            var num = Convert.ToInt64(match.Groups[1].Value);

            switch (match.Groups[2].Value.ToLower())
            {
                case "t": return num * 1024L * 1024L * 1024L * 1024L;
                case "g": return num * 1024L * 1024L * 1024L;
                case "m": return num * 1024L * 1024L;
                case "k": return num * 1024L;
                case "": return num;
            }

            return 0;
        }

        /// <summary>
        /// Format a long file length to pretty file unit
        /// </summary>
        public static string FormatFileSize(long byteCount)
        {
            var suf = new[] { "B", "KB", "MB", "GB", "TB" }; //Longs run out around EB
            if (byteCount == 0) return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt64(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}
