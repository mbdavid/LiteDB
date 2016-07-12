using System;
using System.IO;

namespace LiteDB.Tests
{
   class TestPlatform
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

      public static string FileWriteAllText(string fileName, string content, string customPath = null)
      {
         var path = customPath ?? Path.GetTempPath();

         Directory.CreateDirectory(path);

         var filePath = path + fileName;

         File.WriteAllText(filePath, content);

         return filePath;
      }

      public static void DeleteFile(string path)
      {
         File.Delete(path);
      }

      public static string FileReadAllText(string path)
      {
         return File.ReadAllText(path);
      }
   }
}
