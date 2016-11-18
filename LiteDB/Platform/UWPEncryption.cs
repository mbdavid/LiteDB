using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace LiteDB.Platform
{
    public class UWPEncryption : IEncryption
    {
        private readonly string _keyDerivationAlgo;
        private static readonly byte[] SALT = new byte[] { 0x16, 0xae, 0xbf, 0x20, 0x01, 0xa0, 0xa9, 0x52, 0x34, 0x1a, 0x45, 0x55, 0x4a, 0xe1, 0x32, 0x1d };

        private readonly IBuffer _ivBuffer;
        private readonly SymmetricKeyAlgorithmProvider _aesProvider;
        private readonly CryptographicKey _key;

        public UWPEncryption(string password)
         : this(password, SymmetricAlgorithmNames.AesCbc, KeyDerivationAlgorithmNames.Pbkdf2Sha256)
        {
        }

        public UWPEncryption(string password, string symmetricKeyAlgo, string keyDerivationAlgo)
        {
            _keyDerivationAlgo = keyDerivationAlgo;

            IBuffer aesKeyMaterial;

            GenerateKeyMaterial(password, 1000, out aesKeyMaterial, out _ivBuffer);

            _aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(symmetricKeyAlgo);
            _key = _aesProvider.CreateSymmetricKey(aesKeyMaterial);
        }

        private void GenerateKeyMaterial(string password, uint iterationCount, out IBuffer keyMaterial, out IBuffer iv)
        {
            // setup KDF parameters for the desired salt and iteration count
            IBuffer saltBuffer = CryptographicBuffer.CreateFromByteArray(SALT);
            KeyDerivationParameters kdfParameters = KeyDerivationParameters.BuildForPbkdf2(saltBuffer, iterationCount);

            // get a KDF provider for PBKDF2, and store the source password in a Cryptographic Key
            KeyDerivationAlgorithmProvider kdf = KeyDerivationAlgorithmProvider.OpenAlgorithm(_keyDerivationAlgo);

            var pwBytes = Encoding.GetEncoding("ASCII").GetBytes(password);
            IBuffer passwordBuffer = CryptographicBuffer.CreateFromByteArray(pwBytes);

            //IBuffer passwordBuffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
            CryptographicKey passwordSourceKey = kdf.CreateKey(passwordBuffer);

            // generate key material from the source password, salt, and iteration count.  Only call DeriveKeyMaterial once,
            // since calling it twice will generate the same data for the key and IV.
            int keySize = 256 / 8;
            int ivSize = 128 / 8;
            uint totalDataNeeded = (uint)(keySize + ivSize);
            IBuffer keyAndIv = CryptographicEngine.DeriveKeyMaterial(passwordSourceKey, kdfParameters, totalDataNeeded);

            // split the derived bytes into a seperate key and IV
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
            IBuffer encrypted = CryptographicEngine.Encrypt(_key, toDecryptBuffer, _ivBuffer);
            var result = encrypted.ToArray();

            return result;
        }

        public byte[] Decrypt(byte[] encryptedValue)
        {
            // Convert the base64 input to an IBuffer for decryption
            IBuffer ciphertext = CryptographicBuffer.CreateFromByteArray(encryptedValue);

            // Decrypt the data and convert it back to a string
            IBuffer decrypted = CryptographicEngine.Decrypt(_key, ciphertext, _ivBuffer);
            byte[] decryptedArray = decrypted.ToArray();

            return decryptedArray;
        }

        public byte[] HashSHA1(string str)
        {
            // convert the message string to binary data.
            IBuffer buffUtf8Msg = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);

            // create a HashAlgorithmProvider object.
            HashAlgorithmProvider objAlgProv = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);

            // hash the message.
            IBuffer buffHash = objAlgProv.HashData(buffUtf8Msg);

            // verify that the hash length equals the length specified for the algorithm.
            if (buffHash.Length != objAlgProv.HashLength)
            {
                throw new Exception("There was an error creating the hash");
            }

            var sha1 = buffHash.ToArray();

            return sha1;
        }
    }
}