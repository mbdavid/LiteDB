namespace LiteDB;

internal static class IOExceptionExtensions
{
    private const int ERROR_SHARING_VIOLATION = 32;
    private const int ERROR_LOCK_VIOLATION = 33;

    /// <summary>
    /// Detect if exception is an Locked exception
    /// </summary>
    public static bool IsLocked(this IOException ex)
    {
        var errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);

        return 
            errorCode == ERROR_SHARING_VIOLATION ||
            errorCode == ERROR_LOCK_VIOLATION;
    }
}