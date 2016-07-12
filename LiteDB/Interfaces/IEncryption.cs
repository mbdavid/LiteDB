using System;

namespace LiteDB.Interfaces
{
   public interface IEncryption : IDisposable
   {
      byte[] Encrypt(byte[] bytes);
      byte[] Decrypt(byte[] encryptedValue);
   }
}