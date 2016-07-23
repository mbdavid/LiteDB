using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using LiteDB;

namespace LiteDB.Platform
{
    public class FileHandler : IFileHandler
    {
        private string _defaultPath = ".";

        public FileHandler(String defaultPath)
        {
            _defaultPath = defaultPath;
        }

        public Stream OpenFileAsStream(string filename, bool readOnly)
        {
            return new FileStream(Path.Combine(_defaultPath, filename),
               FileMode.Open,
               readOnly ? FileAccess.Read : FileAccess.ReadWrite,
               readOnly ? FileShare.Read : FileShare.None,
               LiteDatabase.PAGE_SIZE);
        }

        public Stream CreateFile(string filename, bool overwritten)
        {
            return new FileStream(Path.Combine(_defaultPath, filename),
                overwritten ? FileMode.Create : FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None, LiteDatabase.PAGE_SIZE);
        }

        public bool FileExists(string filename)
        {
            return File.Exists(Path.Combine(_defaultPath, filename));
        }

        public void DeleteFile(string filename)
        {
            File.Delete(Path.Combine(_defaultPath, filename));
        }

        public void OpenExclusiveFile(string filename, Action<Stream> success)
        {
            try
            {
                using (var stream = File.Open(Path.Combine(_defaultPath, filename), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
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
