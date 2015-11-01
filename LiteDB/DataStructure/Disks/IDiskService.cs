using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LiteDB
{
    internal interface IDiskService : IDisposable
    {
        void Initialize();
        void Lock();
        void Unlock();
        T ReadPage<T>(uint pageID) where T : BasePage, new();
        void WritePages(IEnumerable<BasePage> pages);
    }
}
