using System;

namespace LiteDB
{
    public interface IEncryption : IDisposable
    {
        byte[] Encrypt(byte[] bytes);
        byte[] Decrypt(byte[] encryptedValue);
        byte[] HashSHA1(string str);
    }
}