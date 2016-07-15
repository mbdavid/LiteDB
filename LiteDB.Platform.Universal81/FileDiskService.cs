using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace LiteDB.Universal81
{
    public class FileDiskService : FileDiskServiceBase
    {
       private readonly StorageFolder m_baseFolder;

       public FileDiskService(StorageFolder baseFolder, ConnectionString conn, Logger log) : base(conn, log)
       {
          m_baseFolder = baseFolder;
       }

       private async Task<IRandomAccessStream> CreateFileOrOpen(string filename)
       {
         var file = await m_baseFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

         return await file.OpenAsync(FileAccessMode.ReadWrite);
      }

       protected override Stream CreateStream(string filename)
       {
          var raStream = AsyncHelpers.RunSync(() => CreateFileOrOpen(filename));
         
         return raStream.AsStream(BasePage.PAGE_SIZE);
         
       }

      // protected override void InnerLock()
      // {
      //   bool locked = _lockSemaphore.Wait(_timeout);
      //   if (!locked)
      //   {
      //      LiteException.LockTimeout(_timeout);
      //   }

      //   _lockLength = _stream.Length;
      //   _log.Write(Logger.DISK, "lock datafile");
      //}

      // protected override void InnerUnlock()
      // {
      //   _lockSemaphore.Release();
      //}

       protected override bool FileExists(string filename)
       {
          return AsyncHelpers.RunSync(async () =>
          {
             try
             {
                await m_baseFolder.GetFileAsync(filename);

                return true;
             }
             catch (Exception)
             {
                return false;
             }
          });
       }

       protected override FileDiskServiceBase CreateFileDiskService(ConnectionString connectionString, Logger log)
       {
          return new FileDiskService(m_baseFolder, connectionString, log);
       }

       protected override void DeleteFile(string filepath)
       {
          this.TryExec(() => AsyncHelpers.RunSync(async () =>
          {
             var file = await m_baseFolder.GetFileAsync(filepath);

             await file.DeleteAsync();
          }))
          ;
       }

       protected override void OpenExclusiveFile(string filename, Action<Stream> success)
       {
         try
         {
            using (var stream = CreateStream(filename))
            {
               success(stream);
            }
         }
         catch (Exception)
         {
            // not found OR lock by another process, .... no recovery, do nothing
         }
      }
   

      private SemaphoreSlim _lockSemaphore = new SemaphoreSlim(1);
   }
}
