using System;
using System.IO;
using NUnit.Framework;

namespace LiteDB.Tests
{
   class TestPlatform
   {
      public static string GetTempFilePath(string ext)
      {
         var path = Path.GetTempPath() + "/TestResults/";

         Directory.CreateDirectory(path);

         return path + string.Format("test-{0}.{1}", Guid.NewGuid(), ext);
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

		public static long GetFileSize(string filename)
		{
			return new FileInfo(filename).Length;
		}

      public static string FileReadAllText(string path)
      {
         return File.ReadAllText(path);
      }

}
}

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
   public class TestClassAttribute : TestFixtureAttribute { }

   public class AssemblyInitializeAttribute : TestFixtureSetUpAttribute { }
   public class AssemblyCleanupAttribute : TestFixtureTearDownAttribute { }

   public class ClassInitializeAttribute : SetUpAttribute { }

   public class ClassCleanupAttribute : TearDownAttribute { }

   public class TestMethodAttribute : TestAttribute { }

   public class Assert : NUnit.Framework.Assert { }
}