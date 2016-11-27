using System;

namespace LiteDB_V6
{
    public interface IDiskService : IDisposable
    {
        void Open(bool readOnly);

        void Close();
		
        byte[] ReadPage(uint pageID);
    }
}