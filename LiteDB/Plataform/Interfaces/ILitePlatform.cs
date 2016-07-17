using System;

namespace LiteDB.Plataform
{
    public interface ILitePlatform
    {
        IEncryptionFactory EncryptionFactory { get; }
        IReflectionHandler ReflectionHandler { get; }
        IFileHandler FileHandler { get; }
        void WaitFor(int milliseconds);
    }
}