namespace LiteDB.Engine;

using System.Diagnostics;

/// <summary>
///     Represent a single query featching data from engine
/// </summary>
internal class CursorInfo
{
    public CursorInfo(string collection, Query query)
    {
        Collection = collection;
        Query = query;
    }

    public string Collection { get; }

    public Query Query { get; set; }

    public int Fetched { get; set; }

    public Stopwatch Elapsed { get; } = new Stopwatch();
}