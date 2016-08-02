using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace LiteDB.Platform
{
    // Uses System.IO methods that are exposed in UWP again, and sends them to the background thread.
    // This is less costly than the SynchronizationContext work required by Windows 8.1
    public class FileHandlerUWP : IFileHandler
    {
        private readonly StorageFolder _folder;
        private readonly String _folderPath;

        public FileHandlerUWP(StorageFolder folder)
        {
            _folder = folder;

            _folderPath = folder.Path;
        }

        public Stream CreateFile(string filename, bool overwritten)
        {
            return syncOverAsync<Stream>(() =>
           {
                // TODO Handle overwritten
                return File.Create(Path.Combine(_folderPath, filename));
           });
        }

        public Stream OpenFileAsStream(string filename, bool readOnly)
        {
            return syncOverAsync<Stream>(() =>
           {
                // TODO Handle readOnly
                return File.Open(Path.Combine(_folderPath, filename), FileMode.OpenOrCreate, readOnly ? FileAccess.Read : FileAccess.ReadWrite);
           });
        }

        public bool FileExists(string filename)
        {
            return syncOverAsync<bool>(() =>
           {
               return File.Exists(Path.Combine(_folderPath, filename));
           });
        }

        public void DeleteFile(string filename)
        {
            syncOverAsync(() =>
           {
               File.Delete(Path.Combine(_folderPath, filename));
           });
        }

        public void OpenExclusiveFile(string filename, Action<Stream> success)
        {
            try
            {
                using (var stream = syncOverAsync<Stream>(() => { return File.Open(Path.Combine(_folderPath, filename), FileMode.Open, FileAccess.ReadWrite, FileShare.None); }))
                {
                    success(stream);
                }
            }
            catch (Exception)
            {
                // not found OR lock by another process, .... no recovery, do nothing
            }
        }

        // These methods will run the specified Action on a background thread
        // so they will not cause issues with UI
        private void syncOverAsync(Action f)
        {
            Task.Run(f).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private T syncOverAsync<T>(Func<T> f)
        {
            return Task.Run<T>(f).ConfigureAwait(false).GetAwaiter().GetResult();
        }

    }
}