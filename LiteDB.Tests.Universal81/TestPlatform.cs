using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using LiteDB.Platform;
using Windows.Storage;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    public class TestClassAttribute : TestPlatform.UnitTestFramework.TestClassAttribute { }

    public class TestMethodAttribute : TestPlatform.UnitTestFramework.TestMethodAttribute { }

    public class Assert : TestPlatform.UnitTestFramework.Assert { }
}

namespace LiteDB.Tests
{
    public class TestBase
    {
        public TestBase()
        {
            LitePlatform.Initialize(new LitePlatformWindowStore());
        }
    }

    public class TestPlatform
    {
        public static string GetTempFilePath(string ext)
        {
            var folder = ApplicationData.Current.TemporaryFolder;

            return string.Format(folder.Path + @"\test-{0}.{1}", Guid.NewGuid(), ext);
        }

        public static long GetFileSize(string filename)
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            var file = AsyncHelpers.RunSync(folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists));

            var properties = AsyncHelpers.RunSync(file.GetBasicPropertiesAsync());

            return (long)properties.Size;
        }

        public static string FileWriteAllText(string fileName, string content)
        {

            if (!string.IsNullOrEmpty(customPath))
            {
                folder = AsyncHelpers.RunSync(folder.CreateFolderAsync(customPath, CreationCollisionOption.OpenIfExists));
            }

            var file = AsyncHelpers.RunSync(folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists));

            var bytes = Encoding.UTF8.GetBytes(content);

            AsyncHelpers.RunSync(async () => await FileIO.WriteBytesAsync(file, bytes));

            return file.Path;
        }

        public static void DeleteFile(string path)
        {
            var folder = ApplicationData.Current.TemporaryFolder;

            path = path.Replace(folder.Path, "");

            var file = AsyncHelpers.RunSync(ApplicationData.Current.TemporaryFolder.GetFileAsync(path));

            file.DeleteAsync();
        }

        public static string FileReadAllText(string path)
        {

            var file = AsyncHelpers.RunSync(ApplicationData.Current.TemporaryFolder.GetFileAsync(path));

            var buffer = AsyncHelpers.RunSync(FileIO.ReadBufferAsync(file));

            var arr = buffer.ToArray();

            var res = Encoding.UTF8.GetString(arr, 0, arr.Length);

            return res;

        }
    }
}