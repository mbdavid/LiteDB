namespace LiteDB.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

internal class TryCatch
{
    public readonly List<Exception> Exceptions = new List<Exception>();

    public TryCatch()
    {
    }

    public TryCatch(Exception initial)
    {
        Exceptions.Add(initial);
    }

    public bool InvalidDatafileState => Exceptions.Any(
        ex =>
            ex is LiteException liteEx &&
            liteEx.ErrorCode == LiteException.INVALID_DATAFILE_STATE);

    [DebuggerHidden]
    public void Catch(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Exceptions.Add(ex);
        }
    }
}