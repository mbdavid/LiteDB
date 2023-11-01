namespace LiteDB;

/// <summary>
/// Static methods for test (in Debug mode) some parameters - ideal to debug database
/// </summary>
[DebuggerStepThrough]
internal static class CodeContract
{
    /// <summary>
    /// If first test is true, ensure second condition to be true, otherwise throw exception (check contract)
    /// </summary>
    [Conditional("DEBUG")]
    public static void ENSURE(bool ifTest, bool condition, string? message = null, object? debugArgs = null)
    {
        if (ifTest && condition == false)
        {
            ShowError(message, null);
        }
    }

    /// <summary>
    /// Ensure condition is true, otherwise throw exception (check contract)
    /// </summary>
    [Conditional("DEBUG")]
    public static void ENSURE(bool condition, string message, object? debugArgs = null)
    {
        if (condition == false)
        {
            ShowError(message, debugArgs);
        }
    }

    /// <summary>
    /// Ensure condition is true, otherwise throw exception (check contract)
    /// </summary>
    [Conditional("DEBUG")]
    public static void ENSURE(bool condition, object? debugArgs = null)
    {
        if (condition == false)
        {
            ShowError(null, debugArgs);
        }
    }

    /// <summary>
    /// Build a pretty error message with debug informations. Used only for DEBUG
    /// </summary>
    private static void ShowError(string? message = null, object? debugArgs = default)
    {
        var st = new StackTrace();
        var frame = st.GetFrame(2);
        var method = frame?.GetMethod();

        // crazy way to detect name when async/sync
        var location = $"{method?.DeclaringType?.DeclaringType?.CleanName()}.{method?.DeclaringType?.CleanName()}.{method?.Name}";

        location = Regex.Replace(location, @"^\.", "");
        location = Regex.Replace(location, @"\.MoveNext", "");

        var err = new StringBuilder($"Error at '{location}'. ");

        if (message is not null)
        {
            err.Append(message + ". ");
        }

        if (debugArgs is not null)
        {
            err.Append(Dump.Object(debugArgs));
        }

        var msg = err.ToString().Trim();

        if (Debugger.IsAttached)
        {
            Debug.Fail(msg);
        }
        else
        {
            throw ERR_ENSURE(err.ToString());
        }
    }
}