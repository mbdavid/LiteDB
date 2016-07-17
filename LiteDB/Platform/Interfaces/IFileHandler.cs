using System;
using System.IO;

namespace LiteDB.Platform
{
    public interface IFileHandler
    {
        Stream OpenFileAsStream(string filename, bool readOnly);
        Stream CreateFile(string filename, bool overwritten);

        bool FileExists(string filename);
        void DeleteFile(string filename);
        void OpenExclusiveFile(string filename, Action<Stream> success);
    }
}