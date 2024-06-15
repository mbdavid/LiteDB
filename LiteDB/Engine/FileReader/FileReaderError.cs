namespace LiteDB.Engine;

using System;

/// <summary>
/// </summary>
internal class FileReaderError
{
    public DateTime Created { get; } = DateTime.Now;
    public FileOrigin Origin { get; set; }
    public long Position { get; set; }
    public uint? PageID { get; set; }
    public PageType PageType { get; set; }
    public string Collection { get; set; }
    public string Message { get; set; }
    public Exception Exception { get; set; }
}