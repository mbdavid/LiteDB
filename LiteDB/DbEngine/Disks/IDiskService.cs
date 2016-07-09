using System;

namespace LiteDB
{
    public interface IDiskService : IDisposable
    {
        bool Initialize();

        void CreateNew();

        void Open(bool readOnly);

        void Close();

        void WriteJournal(uint pageID, byte[] original);

        void DeleteJournal();

        byte[] ReadPage(uint pageID);

        void WritePage(uint pageID, byte[] buffer);

        void SetLength(long fileSize);

        IDiskService GetTempDisk();

        void DeleteTempDisk();
    }
}