namespace LiteDB.Engine;

internal class ExplainPlainBuilder
{
    private List<(string info, int deep)> _infos = new();

    public void Add(string info, int deep)
    {
        _infos.Add((info, deep));
    }

    public BsonArray ToBsonArray()
    {
        var deeps = _infos.Max(x => x.deep);
        var lines = _infos
            .OrderByDescending(x => x.deep)
            .Select(x => "".PadRight(deeps - x.deep, ' ') + "> " + x.info)
            .Select(x => new BsonString(x))
            .ToArray();

        return BsonArray.FromArray(lines);
    }
}
