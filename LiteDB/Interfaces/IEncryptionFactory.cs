namespace LiteDB.Interfaces
{
   public interface IEncryptionFactory
   {
      IEncryption CreateEncryption(string password);
      byte[] HashSHA1(string str);
   }
}