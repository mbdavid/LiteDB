using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;

namespace LiteDB.Plataform
{
    public class FileHandler : IFileHandler
    {
        public Stream OpenFileAsStream(string filename, bool readOnly)
        {
            return new FileStream(filename,
               FileMode.Open,
               readOnly ? FileAccess.Read : FileAccess.ReadWrite,
               readOnly ? FileShare.Read : FileShare.None,
               LiteDatabase.PAGE_SIZE);
        }

        public Stream CreateFile(string filename, bool overwritten)
        {
            return new FileStream(filename,
                overwritten ? FileMode.Create : FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None, LiteDatabase.PAGE_SIZE);
        }

        public bool FileExists(string filename)
        {
            return File.Exists(filename);
        }

        public void DeleteFile(string filename)
        {
            File.Delete(filename);
        }

        public void OpenExclusiveFile(string filename, Action<Stream> success)
        {
            try
            {
                using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
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
