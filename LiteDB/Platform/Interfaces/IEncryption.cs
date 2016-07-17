using System;

namespace LiteDB.Platform
{
    public interface IEncryption : IDisposable
    {
        byte[] Encrypt(byte[] bytes);
        byte[] Decrypt(byte[] encryptedValue);
        byte[] HashSHA1(string str);
    }
}