using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LiteDB
{
    public interface IDiskService : IDisposable
    {
        bool Initialize();

        void Lock();
        void Unlock();

        void WriteJournal(uint pageID, byte[] original);
        void CommitJournal(long fileSize);
        void DeleteJournal();

        byte[] ReadPage(uint pageID);
        void WritePage(uint pageID, byte[] buffer);

        ushort GetChangeID();

        IDiskService GetTempDisk();
        void DeleteTempDisk();
    }
}
