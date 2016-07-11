using System;
using System.IO;
using Windows.Storage;
using LiteDB.Interfaces;

namespace LiteDB.Universal81
{
   public class FileHandlerWindowsStore : IFileHandler
   {
      private readonly StorageFolder m_folder;

      public FileHandlerWindowsStore(StorageFolder folder)
      {

         m_folder = folder;
      }

      public Stream ReadFileAsStream(string filename)
      {
         var raStream = AsyncHelpers.RunSync(async () =>
         {
            filename = filename.Replace(m_folder.Path, "");

            var file = await m_folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

            return await file.OpenAsync(FileAccessMode.ReadWrite);
         });

         return raStream.AsStream();
      }

      public Stream CreateFile(string filename, bool overwritten)
      {
         var raStream = AsyncHelpers.RunSync(async () =>
          {
             filename = filename.Replace(m_folder.Path, "");

             var file = await m_folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

             return await file.OpenAsync(FileAccessMode.ReadWrite);
          });

         return raStream.AsStream();
      }
   }
}