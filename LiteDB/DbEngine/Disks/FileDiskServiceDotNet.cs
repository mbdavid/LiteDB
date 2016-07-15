using System;
using System.IO;
using System.Threading;

namespace LiteDB
{
   public class FileDiskServiceDotNet : FileDiskServiceBase
   {
      public FileDiskServiceDotNet(ConnectionString conn, Logger log) : base(conn, log)
      {
      }

      protected override Stream CreateStream(string filename)
      {
         // open data file (r/w or r)
         return new FileStream(filename,
            _readonly ? FileMode.Open : FileMode.OpenOrCreate,
            _readonly ? FileAccess.Read : FileAccess.ReadWrite,
            _readonly ? FileShare.Read : FileShare.ReadWrite,
            BasePage.PAGE_SIZE);
      }

      //protected override void InnerLock()
      //{
      //   TryExec(() =>
      //      {
      //         _lockLength = _stream.Length;
      //         _log.Write(Logger.DISK, "lock datafile");
      //         var fileStream = _stream as FileStream;
      //         if (fileStream != null) fileStream.Lock(0, _lockLength);
      //      });
      //}

      //protected override void InnerUnlock()
      //{
      //   var fileStream = _stream as FileStream;
      //   if (fileStream != null) fileStream.Unlock(0, _lockLength);
      //}

      protected override bool FileExists(string filename)
      {
         return File.Exists(filename);
      }

      protected override FileDiskServiceBase CreateFileDiskService(ConnectionString connectionString, Logger log)
      {
         return new FileDiskServiceDotNet(connectionString, log);
      }

      protected override void DeleteFile(string filepath)
      {
         TryExec(() => File.Delete(filepath));
      }

      protected override void OpenExclusiveFile(string filename, Action<Stream> success)
      {
         try
         {
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
               success(stream);
            }
         }
         catch (Exception)
         {
            // not found OR lock by another process, .... no recovery, do nothing
         }
      }
   }
}