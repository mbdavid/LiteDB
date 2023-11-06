namespace LiteDB; // the "Engine" sufix name was not used to maintain compatibility with previous versions

/// <summary>
/// All exceptions from LiteDB
/// </summary>
public partial class LiteException
{
    internal static LiteException ERR(string message) =>
        new(0, message);

    internal static LiteException ERR_FILE_NOT_FOUND(string filename) =>
        new(1, $"File '{filename}' not found.");

    internal static LiteException ERR_TOO_LARGE_VARIANT() =>
        new(2, $"Content too large and exceed 1Gb limit.");

    internal static LiteException ERR_TIMEOUT(TimeSpan timeout) =>
        new(3, $"Timeout exceeded. Limit: {timeout.TotalSeconds:0}");

    internal static InvalidOperationException ERR_READONLY_OBJECT() =>
        new($"This object are marked as readonly and can't be changed");

    internal static LiteException ERR_INVALID_CTOR(Type type, Exception? inner) =>
        new(4, $"Failed to create instance for type `{type.FullName}` from assembly `{type.FullName}`. Checks if the class has a public constructor with no parameters.", inner);


    #region ERR_UNEXPECTED_TOKEN

    internal static LiteException ERR_UNEXPECTED_TOKEN(Token token, string? expected = null)
    {
        var position = token.Position;

        var sb = StringBuilderCache.Acquire();

        sb.Append("Unexpected token `");

        if (token.Type == TokenType.EOF)
        {
            sb.Append("[EOF]");
        }
        else
        {
            sb.Append(token.Value);
        }

        sb.Append($"` at position {token.Position}.`");

        if (expected is not null)
        {
            sb.AppendFormat(" Expected `{0}`.", expected);
        }

        return new (20, StringBuilderCache.Release(sb))
        {
            Position = position
        };
    }

    #endregion

    #region CRITITAL ERRORS (stop engine)

    internal static LiteException ERR_INVALID_DATABASE() =>
        new(900, $"File is not a valid LiteDB database format or contains a invalid password.");

    internal static LiteException ERR_INVALID_FILE_VERSION() =>
        new(900, $"Invalid database file version.");

    internal static LiteException ERR_INVALID_FREE_SPACE_PAGE(uint pageID, int freeBytes, int length) =>
        new(901, $"An operation that would corrupt pageID #{pageID} was prevented. The operation required {length} free bytes, but the page had only {freeBytes} available.");

    internal static LiteException ERR_ENSURE(string? message) =>
        new(902, $"ENSURE: {message}.");

    internal static LiteException ERR_DATAFILE_NOT_ENCRYPTED() =>
        new(903, $"This datafile are not encrypted and shoutn't provide password");

    internal static LiteException ERR_DISK_WRITE_FAILURE(Exception ex) =>
        new(904, $"Disk fail in write operation: {ex.Message}. See inner exception for details", ex);

    #endregion
}