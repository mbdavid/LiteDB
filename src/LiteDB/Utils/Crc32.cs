namespace LiteDB;

/// <summary>
/// </summary>
internal static class Crc32
{
    /// <summary>
    /// Static initializer _table
    /// </summary>
    static Crc32()
    {
    }

    public unsafe static int ComputeChecksum(PageMemory* page)
    {
        // calculate crc32 over content only (skip header)
        var contentArea = new Span<byte>((byte*)(nint)page + PAGE_HEADER_SIZE, PAGE_CONTENT_SIZE);

        return ComputeChecksum(contentArea);
    }

    public static int ComputeChecksum(Span<byte> span)
    {
        using var _pc = PERF_COUNTER(170, nameof(ComputeChecksum), nameof(Crc32));

        return 0;
    }
}