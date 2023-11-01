namespace LiteDB;

/// <summary>
/// Calculate CRC-8 (1 byte)
/// https://blog.csdn.net/plutus_sutulp/article/details/8473377
/// </summary>
internal static class Crc8
{
    static byte[] _table = new byte[256];
    // x8 + x7 + x6 + x4 + x2 + 1
    const byte poly = 0xd5;

    /// <summary>
    /// Static initializer _table
    /// </summary>
    static Crc8()
    {
        for (var i = 0; i < 256; ++i)
        {
            var temp = i;

            for (var j = 0; j < 8; ++j)
            {
                if ((temp & 0x80) != 0)
                {
                    temp = (temp << 1) ^ poly;
                }
                else
                {
                    temp <<= 1;
                }
            }

            _table[i] = (byte)temp;
        }
    }

    public static byte ComputeChecksum(Span<byte> span)
    {
        using var _pc = PERF_COUNTER(170, nameof(ComputeChecksum), nameof(Crc8));

        byte crc = 0;

        for (var i = 0; i < span.Length; i++)
        {
            var b = span[i];

            crc = _table[crc ^ b];
        }

        return crc;
    }
}