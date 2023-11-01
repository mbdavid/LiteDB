namespace LiteDB;

internal static class HashSetExtensions
{
    public static HashSet<T> AddRange<T>(this HashSet<T> hash, IEnumerable<T> items)
    {
        if (items is null) return hash;

        foreach(var item in items)
        {
            hash.Add(item);
        }

        return hash;
    }
}