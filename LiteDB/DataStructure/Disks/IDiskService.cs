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
        void Initialize();
        void Lock();
        void Unlock();
        byte[] ReadPage(uint pageID);
        void ChangePage(uint pageID, byte[] original);
        ushort GetChangeID();
        void StartWrite();
        void WritePage(uint pageID, byte[] buffer);
        void EndWrite();
    }
}
