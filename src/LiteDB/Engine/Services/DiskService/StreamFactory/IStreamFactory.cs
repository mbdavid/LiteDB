namespace LiteDB.Engine;

internal interface IStreamFactory
{
    string Name { get; }
    bool Exists();
    long GetLength();
    void Delete();
    Stream GetStream(bool canWrite, FileOptions options);
    bool DisposeOnClose { get; }
}
