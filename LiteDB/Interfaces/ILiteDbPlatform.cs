namespace LiteDB.Interfaces
{
   public interface ILiteDbPlatform
   {
      FileDiskServiceBase CreateFileDiskService(ConnectionString conn, Logger log);

      IEncryptionFactory EncryptionFactory { get; }
      IReflectionHandler ReflectionHandler { get; }
      IFileHandler FileHandler { get; }

      void WaitFor(int milliseconds);
   }
}