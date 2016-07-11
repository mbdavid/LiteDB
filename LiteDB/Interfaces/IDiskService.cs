using System;

namespace LiteDB.Interfaces
{
   public interface IDiskService : IDisposable
   {
      bool Initialize();

      void CreateNew();

      void WriteJournal(uint pageID, byte[] original);

      void DeleteJournal();

      byte[] ReadPage(uint pageID);

      void WritePage(uint pageID, byte[] buffer);

      void SetLength(long fileSize);

      IDiskService GetTempDisk();

      void DeleteTempDisk();

      void Open(bool readOnly);
      void Close();
   }
}