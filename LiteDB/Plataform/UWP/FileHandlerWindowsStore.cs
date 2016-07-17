using System;
using System.IO;
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

        public Stream ReadFileAsStream(string filename)
        {
            var raStream = AsyncHelpers.RunSync(async () =>
            {
                filename = filename.Replace(_folder.Path, "");

                var file = await _folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                return await file.OpenAsync(FileAccessMode.ReadWrite);
            });

            return raStream.AsStream();
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
    }
}