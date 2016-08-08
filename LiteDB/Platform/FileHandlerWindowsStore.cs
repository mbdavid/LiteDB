using System;
using System.IO;
using LiteDB;
using Windows.Storage;

namespace LiteDB.Platform
{
    public class FileHandlerWindowsStore : IFileHandler
    {
        private readonly StorageFolder _folder;

        public FileHandlerWindowsStore(StorageFolder folder)
        {
            _folder = folder;
        }

        public Stream CreateFile(string filename, bool overwritten)
        {
            var raStream = AsyncHelpers.RunSync(async () =>
            {
                filename = filename.Replace(_folder.Path, "");

                var file = await _folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                return await file.OpenAsync(FileAccessMode.ReadWrite);
            });

            return raStream.AsStream();
        }

        public Stream OpenFileAsStream(string filename, bool readOnly)
        {
            var raStream = AsyncHelpers.RunSync(async () =>
            {
                filename = filename.Replace(_folder.Path, "");

                var file = await _folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                return await file.OpenAsync(FileAccessMode.ReadWrite);
            });

            return raStream.AsStream();
        }

        public bool FileExists(string filename)
        {
            return AsyncHelpers.RunSync(async () =>
            {
                try
                {
                    await _folder.GetFileAsync(filename);

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        public void DeleteFile(string filename)
        {
            AsyncHelpers.RunSync(async () =>
            {
                var file = await _folder.GetFileAsync(filename);

                await file.DeleteAsync();
            });
        }

        public void OpenExclusiveFile(string filename, Action<Stream> success)
        {
            try
            {
                using (var stream = OpenFileAsStream(filename, true))
                {
                    success(stream);
                }
            }
            catch (Exception)
            {
                // not found OR lock by another process, .... no recovery, do nothing
            }
        }
    }
}