using System;

namespace LiteDB.Platform
{
    public interface ILitePlatform
    {
        IReflectionHandler ReflectionHandler { get; }
        IFileHandler FileHandler { get; }
        IEncryption GetEncryption(string password);
        void WaitFor(int milliseconds);
    }
}