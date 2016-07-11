using Windows.Security.Cryptography.Core;
using LiteDB.Interfaces;

namespace LiteDB.Universal81
{
   public class EncryptionFactory : IEncryptionFactory
   {
      private readonly string m_symmetricKeyAlgo;
      private readonly string m_keyDerivationAlgo;

      public EncryptionFactory()
      {
         m_symmetricKeyAlgo = SymmetricAlgorithmNames.AesCbc;
         m_keyDerivationAlgo = KeyDerivationAlgorithmNames.Pbkdf2Sha256;
      }

      public EncryptionFactory(string symmetricKeyAlgo, string keyDerivationAlgo)
      {
         m_symmetricKeyAlgo = symmetricKeyAlgo;
         m_keyDerivationAlgo = keyDerivationAlgo;
      }

      public IEncryption CreateEncryption(string password)
      {
         return new SimpleAES(password, m_symmetricKeyAlgo, m_keyDerivationAlgo);
      }

      public byte[] HashSHA1(string str)
      {
         return SimpleAES.HashSHA1(str);
      }
   }
}