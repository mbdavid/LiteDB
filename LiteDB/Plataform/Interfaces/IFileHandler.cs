using System;
using System.IO;

namespace LiteDB.Plataform
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