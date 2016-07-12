using System;
using System.IO;
using LiteDB.Core;
using LiteDB.Interfaces;

namespace LiteDB.Platform.iOS
{
   public class LiteDbPlatformiOS : LiteDbPLatformBase
   {
      public override FileDiskServiceBase CreateFileDiskService(ConnectionString conn, Logger log)
      {
         return new FileDiskServiceDotNet(conn, log);
      }

      public LiteDbPlatformiOS(IEncryptionFactory encryptionFactory, IReflectionHandler reflectionHandler, IFileHandler fileHandler)
         : base(() => encryptionFactory, () => reflectionHandler, () => fileHandler)
      {
      }

      public LiteDbPlatformiOS() : base(() => new EncryptionFactory(),
         () => new ExpressionReflectionHandler(), () => new FileHandler())
      {
      }
   }

   public class FileHandler : IFileHandler
   {
		public Stream CreateFile(string file, bool overwritten)
		{
			return File.Create(file);
		}

		public Stream ReadFileAsStream(string filename)
      {
         return File.OpenRead(filename);
      }
   }

   public class EncryptionFactory : IEncryptionFactory
   {
      public IEncryption CreateEncryption(string password)
      {
         return new SimpleAES(password);
      }

      public byte[] HashSHA1(string str)
      {
         return SimpleAES.HashSHA1(str);
      }
   }
}
