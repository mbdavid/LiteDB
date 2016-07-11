#if !NETCORE && !PCL
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using LiteDB.Interfaces;

namespace LiteDB
{
    /// <summary>
    /// Simple Rijndael wrapper to encrypt data pages (based in http://stackoverflow.com/questions/165808/simple-two-way-encryption-for-c-sharp)
    /// </summary>
    internal class SimpleAES : IEncryption
    {
        private static readonly byte[] SALT = new byte[] { 0x16, 0xae, 0xbf, 0x20, 0x01, 0xa0, 0xa9, 0x52, 0x34, 0x1a, 0x45, 0x55, 0x4a, 0xe1, 0x32, 0x1d };

        private Rijndael _rijndael;

      public static string ToHex( byte[] bytes)
      {
         char[] c = new char[bytes.Length * 2];

         byte b;

         for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
         {
            b = ((byte)(bytes[bx] >> 4));
            c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

            b = ((byte)(bytes[bx] & 0x0F));
            c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
         }

         return new string(c);
      }

      public SimpleAES(string password)
        {
            _rijndael = Rijndael.Create();
            _rijndael.Padding = PaddingMode.Zeros;
            Rfc2898DeriveBytes pdb = null;
            try
            {
            
                pdb = new Rfc2898DeriveBytes(password, SALT);
                _rijndael.Key = pdb.GetBytes(32);
            Debug.WriteLine("Key = "+ ToHex(_rijndael.Key));
                _rijndael.IV = pdb.GetBytes(16);

            Debug.WriteLine("IV = " + ToHex(_rijndael.IV));
         }
            finally
            {
                IDisposable disp = pdb as IDisposable;

                if (disp != null)
                {
                    disp.Dispose();
                }
            }
        }

        /// <summary>
        /// Encrypt byte array returning new encrypted byte array with same length of original array (PAGE_SIZE)
        /// </summary>
        public byte[] Encrypt(byte[] bytes)
        {
            using (var encryptor = _rijndael.CreateEncryptor())
            using (var stream = new MemoryStream())
            using (var crypto = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
            {
                crypto.Write(bytes, 0, bytes.Length);
                crypto.FlushFinalBlock();
                stream.Position = 0;
                var encrypted = new byte[stream.Length];
                stream.Read(encrypted, 0, encrypted.Length);


            Debug.WriteLine("Decrypted: "+ ToHex(bytes));
            Debug.WriteLine("To Encrypted: "+ ToHex(encrypted));

                return encrypted;
            }
        }

        /// <summary>
        /// Decrypt and byte array returning a new byte array
        /// </summary>
        public byte[] Decrypt(byte[] encryptedValue)
        {
            using (var decryptor = _rijndael.CreateDecryptor())
            using (var stream = new MemoryStream())
            using (var crypto = new CryptoStream(stream, decryptor, CryptoStreamMode.Write))
            {
                crypto.Write(encryptedValue, 0, encryptedValue.Length);
                crypto.FlushFinalBlock();
                stream.Position = 0;
                var decryptedBytes = new Byte[stream.Length];
                stream.Read(decryptedBytes, 0, decryptedBytes.Length);


            Debug.WriteLine("Encrypted: " + ToHex(encryptedValue));
            Debug.WriteLine("To Decrypted: " + ToHex(decryptedBytes));


            return decryptedBytes;
            }
        }

        /// <summary>
        /// Hash a password using SHA1 just to verify password
        /// </summary>
        public static byte[] HashSHA1(string password)
        {
            var sha = new SHA1CryptoServiceProvider();
            var shaBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

         Debug.WriteLine("Sha1 "+ ToHex(shaBytes));

         return shaBytes;
        }

        public void Dispose()
        {
            if (_rijndael != null)
            {
                _rijndael.Clear();
                _rijndael = null;
            }
        }
    }
}
#endif