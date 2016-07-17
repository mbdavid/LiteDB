using System;
using System.IO;
using LiteDB.Platform;
using NUnit.Framework;

namespace LiteDB.Tests
{
    public class TestBase
    {
        public TestBase()
        {
            LitePlatform.Initialize(new LitePlatformAndroid());
        }
    }

    public class TestPlatform
    {
        public static string GetTempFilePath(string ext)
        {
            var path = Path.GetTempPath() + "TestResults/";

            Directory.CreateDirectory(path);

            return path + string.Format("test-{0}.{1}", Guid.NewGuid(), ext);
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