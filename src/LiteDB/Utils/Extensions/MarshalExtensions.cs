namespace LiteDB;

internal unsafe static class MarshalEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFullZero(PageMemory* page)
    {
        var span = new Span<byte>((byte*)(nint)page, PAGE_SIZE);

        return span.IsFullZero();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillZero(byte* ptr, int length)
    {
        var span = new Span<byte>(ptr, length);

        span.Fill(0);
    }
}