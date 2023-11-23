namespace LiteDB.Engine;

/// <summary>
/// A thread-safe interface for file disk access
/// </summary>
unsafe internal partial interface IDisk : IDisposable
{
    /// <summary>
    /// Get if file is already open/created
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Get filename
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Open file handle
    /// </summary>
    void Open();

    /// <summary>
    /// Create new file (do not override)
    /// </summary>
    void CreateNew();

    /// <summary>
    /// Read a buffer content from file using absolute position. File must be opened
    /// </summary>
    bool ReadBuffer(Span<byte> buffer, long position);

    /// <summary>
    /// Write buffer content into file using absolute position. File must be opened
    /// </summary>
    void WriteBuffer(Span<byte> buffer, long position);

    /// <summary>
    /// Returns if file already exists in disk
    /// </summary>
    bool Exists();

    /// <summary>
    /// Get file length
    /// </summary>
    long GetLength();

    /// <summary>
    /// Set a new file length. Can crop file or add \0
    /// </summary>
    void SetLength(long position);
}
