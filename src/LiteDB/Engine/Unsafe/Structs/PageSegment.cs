namespace LiteDB.Engine;

/// <summary>
/// Represent a single page segment with Location (position) and Length
/// </summary>
unsafe internal struct PageSegment : IIsEmpty
{
    public ushort Location;  // 2
    public ushort Length;    // 2

    /// <summary>
    /// Get final location (Location + Length)
    /// </summary>
    public ushort EndLocation => (ushort)(this.Location + this.Length);

    /// <summary>
    /// Indicate this segment are clear (no reference)
    /// </summary>
    public bool IsEmpty => this.Location == 0 && this.Length == 0;

    public PageSegment()
    {
    }

    public PageSegment(ushort location, ushort length)
    {
        this.Location = location;
        this.Length = length;
    }

    public void Reset()
    {
        this.Location = 0;
        this.Length = 0;
    }

    public Span<byte> AsSpan(PageMemory* page)
    {
        ENSURE(!this.IsEmpty);

        var ptr = (byte*)((nint)page + this.Location);

        return new Span<byte>(ptr, this.Length);
    }

    public override string ToString() => Dump.Object(this);

}
