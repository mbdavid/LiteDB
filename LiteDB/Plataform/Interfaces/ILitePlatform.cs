using System;

namespace LiteDB.Plataform
{
    public interface ILitePlatform
    {
        IReflectionHandler ReflectionHandler { get; }
        IFileHandler FileHandler { get; }
        IEncryption GetEncryption(string password);
        void WaitFor(int milliseconds);
    }
}