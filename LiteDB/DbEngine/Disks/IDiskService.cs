using System;

namespace LiteDB
{
    public interface IDiskService : IDisposable
    {
        bool Initialize();

        void CreateNew();

        void Lock();

        void Unlock();

        void WriteJournal(uint pageID, byte[] original);

        void DeleteJournal();

        byte[] ReadPage(uint pageID);

        void WritePage(uint pageID, byte[] buffer);

        void SetLength(long fileSize);

        ushort GetChangeID();

        IDiskService GetTempDisk();

        void DeleteTempDisk();
    }
}