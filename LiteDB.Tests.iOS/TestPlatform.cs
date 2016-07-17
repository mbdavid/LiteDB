using System;
using System.IO;
using LiteDB.Platform;

namespace LiteDB.Tests
{
    public class TestBase
    {
        public TestBase()
        {
            LitePlatform.Initialize(new LitePlatformiOS());
        }
    }

    public class TestPlatform
    {
        private static string _path;

        static TestPlatform()
        {
            _path = Path.GetFullPath(Directory.GetCurrentDirectory() + "../../../../TestResults/");
        }

        public static string GetFullPath(string filename)
        {
            return _path + filename;
        }

        public static long GetFileSize(string filename)
        {
            return new FileInfo(_path + filename).Length;
        }

        public static void FileWriteAllText(string filename, string content)
        {
            File.WriteAllText(_path + filename, content);
        }

        public static void DeleteFile(string filename)
        {
            File.Delete(_path + filename);
        }

        public static string FileReadAllText(string filename)
        {
            return File.ReadAllText(_path + filename);
        }
    }
}

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    public class TestClassAttribute : NUnit.Framework.TestFixtureAttribute { }
    public class AssemblyInitializeAttribute : NUnit.Framework.TestFixtureSetUpAttribute { }
    public class AssemblyCleanupAttribute : NUnit.Framework.TestFixtureTearDownAttribute { }
    public class ClassInitializeAttribute : NUnit.Framework.SetUpAttribute { }
    public class ClassCleanupAttribute : NUnit.Framework.TearDownAttribute { }
    public class TestMethodAttribute : NUnit.Framework.TestAttribute { }
    public class Assert : NUnit.Framework.Assert { }
}