namespace LiteDB.Engine;

internal class FileStreamFactory : IStreamFactory
{
    private readonly string _filename;
    private readonly bool _readOnly;

    public string Name => Path.GetFileName(_filename);

    public FileStreamFactory(string filename, bool readOnly)
    {
        _filename = filename;
        _readOnly = readOnly;
    }

    public void Delete()
    {
        File.Delete(_filename);
    }

    public bool Exists()
    {
        return File.Exists(_filename);
    }

    public long GetLength()
    {
        // if file don't exists, returns 0
        if (!this.Exists()) return 0;

        // get physical file length from OS
        var length = new FileInfo(_filename).Length;

        return length;
    }

    /// <summary>
    /// Open an existing data file and return FileStream implementation
    /// </summary>
    public Stream GetStream(bool canWrite, FileOptions options)
    {
        var write = canWrite && (_readOnly == false);

        var stream = new FileStream(
            _filename,
            _readOnly ? FileMode.Open : FileMode.OpenOrCreate,
            write ? FileAccess.ReadWrite : FileAccess.Read,
            write ? FileShare.Read : FileShare.ReadWrite,
            PAGE_SIZE,
            options);

        return stream;
    }

    public bool DisposeOnClose => true;
}
