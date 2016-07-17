using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;

namespace LiteDB.Plataform
{
    public class RijndaelEncryptionFactory : IEncryptionFactory
    {
        public IEncryption CreateEncryption(string password)
        {
            return new RijndaelEncryption(password);
        }

        public byte[] HashSHA1(string str)
        {
            return RijndaelEncryption.HashSHA1(str);
        }
    }
}
