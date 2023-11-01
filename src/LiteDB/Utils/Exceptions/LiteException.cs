namespace LiteDB;

/// <summary>
/// The main exception for LiteDB
/// </summary>
public partial class LiteException : Exception
{
    public int ErrorCode { get; }
    public long Position { get; private set; }

    public bool IsCritical => this.ErrorCode >= 900;

    private LiteException(int code, string message)
        : base(message)
    {
        this.ErrorCode = code;
    }

    private LiteException(int code, string message, Exception? inner)
    : base(message, inner)
    {
        this.ErrorCode = code;
    }
}