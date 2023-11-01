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

    //TODO: tenho que consumier o async enumerable pra gerar isso
    //public static ValueTask<BsonValue[]> ToArrayAsync(this IBsonDataReader reader) => ToEnumerableAsync(reader);
    //
    //public static IList<BsonValue> ToListAsync(this IBsonDataReader reader) => ToEnumerable(reader).ToList();
    //
    //public static BsonValue FirstAsync(this IBsonDataReader reader) => ToEnumerable(reader).First();
    //
    //public static BsonValue FirstOrDefaultAsync(this IBsonDataReader reader) => ToEnumerable(reader).FirstOrDefault();
    //
    //public static BsonValue SingleAsync(this IBsonDataReader reader) => ToEnumerable(reader).Single();
    //
    //public static BsonValue SingleOrDefaultAsync(this IBsonDataReader reader) => ToEnumerable(reader).SingleOrDefault();
}