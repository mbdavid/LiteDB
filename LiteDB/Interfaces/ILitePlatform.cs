using System;

namespace LiteDB
{
    public interface ILitePlatform
    {
        IReflectionHandler ReflectionHandler { get; }
        IFileHandler FileHandler { get; }
        IEncryption GetEncryption(string password);
        void WaitFor(int milliseconds);
    }
}