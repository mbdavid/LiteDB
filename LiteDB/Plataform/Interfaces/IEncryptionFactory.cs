using System;

namespace LiteDB.Plataform
{
    public interface IEncryptionFactory
    {
        IEncryption CreateEncryption(string password);
        byte[] HashSHA1(string str);
    }
}