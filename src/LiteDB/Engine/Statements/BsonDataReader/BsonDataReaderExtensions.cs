namespace LiteDB;

/// <summary>
/// Implement some Enumerable methods to IBsonDataReader
/// </summary>
public static class BsonDataReaderExtensions
{
    public static async IAsyncEnumerable<BsonValue> ToEnumerableAsync(this IDataReader reader)
    {
        try
        {
            while (await reader.ReadAsync())
            {
                yield return reader.Current;
            }
        }
        finally
        {
            reader.Dispose();
        }
    }

    public static async ValueTask<IList<BsonValue>> ToListAsync(this IDataReader reader)
    {
        var result = new List<BsonValue>();

        while (await reader.ReadAsync())
        {
            result.Add(reader.Current);
        }

        return result;
    }

    public static async ValueTask<BsonValue[]> ToArrayAsync(this IDataReader reader) => (await ToListAsync(reader)).ToArray();

    //public static BsonValue FirstAsync(this IBsonDataReader reader) => ToEnumerable(reader).First();
    //
    //public static BsonValue FirstOrDefaultAsync(this IBsonDataReader reader) => ToEnumerable(reader).FirstOrDefault();
    //
    //public static BsonValue SingleAsync(this IBsonDataReader reader) => ToEnumerable(reader).Single();
    //
    //public static BsonValue SingleOrDefaultAsync(this IBsonDataReader reader) => ToEnumerable(reader).SingleOrDefault();
}