using static LiteDB.Constants;

namespace LiteDB;

using System;

/// <summary>
///     A singleton shared randomizer class
/// </summary>
internal static class Randomizer
{
    private static readonly Random _random = new Random(RANDOMIZER_SEED);

    public static int Next()
    {
        lock (_random)
        {
            return _random.Next();
        }
    }

    public static int Next(int minValue, int maxValue)
    {
        lock (_random)
        {
            return _random.Next(minValue, maxValue);
        }
    }
}