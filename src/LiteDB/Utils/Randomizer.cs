namespace LiteDB;

/// <summary>
/// A singleton shared randomizer class
/// </summary>
internal static class Randomizer
{
    private static readonly Random _random = new(RANDOMIZER_SEED);

    public static int Next()
    {
#if DEBUG
        lock (_random)
        {
            return _random.Next();
        }
#else
        return Random.Shared.Next();
#endif
    }

    public static int Next(int minValue, int maxValue)
    {
#if DEBUG
        lock (_random)
        {
            return _random.Next(minValue, maxValue);
        }
#else
        return Random.Shared.Next(minValue, maxValue);
#endif
    }
}