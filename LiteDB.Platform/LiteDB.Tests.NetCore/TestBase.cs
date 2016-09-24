using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Platform;
using System.IO;

namespace LiteDB.Tests.NetCore
{
    public class TestBase
    {
        public TestBase()
        {
            LitePlatform.Initialize(new LitePlatformNetCore());
        }
    }

    public class TestPlatform
    {
        public static string GetFullPath(string filename)
        {
            return filename;
        }

        public static long GetFileSize(string filename)
        {
            var fi = new FileInfo(filename);
            return fi.Length;
        }

        public static void FileWriteAllText(string filename, string content)
        {
            File.WriteAllText(filename, content);
        }

        public static void DeleteFile(string filename)
        {
            File.Delete(filename);
        }

        public static string FileReadAllText(string filename)
        {
            return File.ReadAllText(filename);
        }
    }
}
