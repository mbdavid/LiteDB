namespace LiteDB.Engine;

/// <summary>
/// File implementation for sort stream
/// </summary>
internal class FileSortStreamFactory : IStreamFactory
{
    private readonly string _filename;

    public string Name => Path.GetFileName(_filename);

    public FileSortStreamFactory(string dataFilename)
    {
        _filename = FileHelper.GetSufixFile(dataFilename, "-sort", true);
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
    /// Get a new stream (open/create) from sort temp
    /// </summary>
    public Stream GetStream(bool canWrite, FileOptions options)
    {
        var stream = new FileStream(
            _filename,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite,
            PAGE_SIZE,
            FileOptions.Asynchronous);

        return stream;
    }

    public bool DisposeOnClose => true;
}
