using System;
using System.IO;
using LiteDB.Platform;

namespace LiteDB.Tests
{
    public class TestBase
    {
        public TestBase()
        {
            LitePlatform.Initialize(new LitePlatformFullDotNet());
        }
    }

    public class TestPlatform
    {
        public static string GetTempFilePath(string ext)
        {
            return Path.GetFullPath(
                   Directory.GetCurrentDirectory() +
                   string.Format("../../../../TestResults/test-{0}.{1}", Guid.NewGuid(), ext));
        }

        public static long GetFileSize(string filename)
        {
            return new FileInfo(filename).Length;
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
