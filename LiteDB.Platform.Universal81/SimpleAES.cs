using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using LiteDB.Interfaces;

namespace LiteDB.Universal81
{
   public class SimpleAES : IEncryption
   {
      private readonly string m_keyDerivationAlgo;
      private static readonly byte[] SALT = new byte[] { 0x16, 0xae, 0xbf, 0x20, 0x01, 0xa0, 0xa9, 0x52, 0x34, 0x1a, 0x45, 0x55, 0x4a, 0xe1, 0x32, 0x1d };

      private readonly IBuffer m_ivBuffer;
      private readonly SymmetricKeyAlgorithmProvider m_aesProvider;
      private readonly CryptographicKey m_key;

      public SimpleAES(string password) 
         : this(password, SymmetricAlgorithmNames.AesCbc, KeyDerivationAlgorithmNames.Pbkdf2Sha256)  
      {
      }

      public SimpleAES(string password, string symmetricKeyAlgo, string keyDerivationAlgo)
      {
         m_keyDerivationAlgo = keyDerivationAlgo;

         IBuffer aesKeyMaterial;

         GenerateKeyMaterial(password, 1000, out aesKeyMaterial, out m_ivBuffer);

         m_aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(symmetricKeyAlgo);
         m_key = m_aesProvider.CreateSymmetricKey(aesKeyMaterial);
      }
 
      private void GenerateKeyMaterial(string password, uint iterationCount, out IBuffer keyMaterial, out IBuffer iv)
      {
         // Setup KDF parameters for the desired salt and iteration count
         IBuffer saltBuffer = CryptographicBuffer.CreateFromByteArray(SALT);
         KeyDerivationParameters kdfParameters = KeyDerivationParameters.BuildForPbkdf2(saltBuffer, iterationCount);

         // Get a KDF provider for PBKDF2, and store the source password in a Cryptographic Key
         KeyDerivationAlgorithmProvider kdf = KeyDerivationAlgorithmProvider.OpenAlgorithm(m_keyDerivationAlgo);
         
         var pwBytes = Encoding.GetEncoding("ASCII").GetBytes(password);
         IBuffer passwordBuffer = CryptographicBuffer.CreateFromByteArray(pwBytes);

         //IBuffer passwordBuffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
         CryptographicKey passwordSourceKey = kdf.CreateKey(passwordBuffer);

         // Generate key material from the source password, salt, and iteration count.  Only call DeriveKeyMaterial once,
         // since calling it twice will generate the same data for the key and IV.
         int keySize = 256 / 8;
         int ivSize = 128 / 8;
         uint totalDataNeeded = (uint)(keySize + ivSize);
         IBuffer keyAndIv = CryptographicEngine.DeriveKeyMaterial(passwordSourceKey, kdfParameters, totalDataNeeded);

         // Split the derived bytes into a seperate key and IV
         byte[] keyMaterialBytes = keyAndIv.ToArray();

         keyMaterial = WindowsRuntimeBuffer.Create(keyMaterialBytes, 0, keySize, keySize);
         iv = WindowsRuntimeBuffer.Create(keyMaterialBytes, keySize, ivSize, ivSize);
      }
      
      public void Dispose()
      {
      }

      public byte[] Encrypt(byte[] bytes)
      {
         // Create a buffer that contains the encoded message to be encrypted.
         var toDecryptBuffer = CryptographicBuffer.CreateFromByteArray(bytes);


         // Encrypt the data and convert it to a Base64 string
         IBuffer encrypted = CryptographicEngine.Encrypt(m_key, toDecryptBuffer, m_ivBuffer);
         var result= encrypted.ToArray();

         return result;
      }

      public byte[] Decrypt(byte[] encryptedValue)
      {
         // Convert the base64 input to an IBuffer for decryption
         IBuffer ciphertext = CryptographicBuffer.CreateFromByteArray(encryptedValue);

         // Decrypt the data and convert it back to a string
         IBuffer decrypted = CryptographicEngine.Decrypt(m_key, ciphertext, m_ivBuffer);
         byte[] decryptedArray = decrypted.ToArray();

         return decryptedArray;
      }

      public static byte[] HashSHA1(string str)
      {
         // Convert the message string to binary data.
         IBuffer buffUtf8Msg = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);

         // Create a HashAlgorithmProvider object.
         HashAlgorithmProvider objAlgProv = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);

         // Hash the message.
         IBuffer buffHash = objAlgProv.HashData(buffUtf8Msg);

         // Verify that the hash length equals the length specified for the algorithm.
         if (buffHash.Length != objAlgProv.HashLength)
         {
            throw new Exception("There was an error creating the hash");
         }

         var sha1 =  buffHash.ToArray();

         return sha1;
      }
   }
}