using System;
using LiteDB.Interfaces;

namespace LiteDB
{
   public abstract class LiteDbPLatformBase : ILiteDbPlatform
   {
      private readonly Func<IEncryptionFactory> m_getEncryptionFactory;
      private readonly Func<IReflectionHandler> m_getReflectionHandler;
      private readonly Func<IFileHandler> m_getFileHandler;

      protected LiteDbPLatformBase(Func<IEncryptionFactory> getEncryptionFactory,
         Func<IReflectionHandler> getReflectionHandler, Func<IFileHandler> getFileHandler)
      {
         m_getEncryptionFactory = getEncryptionFactory;
         m_getReflectionHandler = getReflectionHandler;
         m_getFileHandler = getFileHandler;
      }

      public abstract FileDiskServiceBase CreateFileDiskService(ConnectionString conn, Logger log);


      private IEncryptionFactory m_encryptionFactory;

      public IEncryptionFactory EncryptionFactory
      {
         get { return m_encryptionFactory ?? (m_encryptionFactory = m_getEncryptionFactory()); }
      }

      private IReflectionHandler m_reflectionHandler;

      public IReflectionHandler ReflectionHandler
      {
         get { return m_reflectionHandler ?? (m_reflectionHandler = m_getReflectionHandler()); }
      }
      private IFileHandler m_fileHandler;

      public IFileHandler FileHandler
      {
         get { return m_fileHandler ?? (m_fileHandler = m_getFileHandler()); }
      }
   }
}