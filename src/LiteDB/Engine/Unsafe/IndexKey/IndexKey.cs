namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, Size = 8, CharSet = CharSet.Ansi)]
unsafe internal partial struct IndexKey
{
    [FieldOffset(0)] public BsonType Type;    // 1
    [FieldOffset(1)] public byte KeyLength;   // 1 // do not include padding
    [FieldOffset(2)] public ushort Reserved;  // 2

    [FieldOffset(4)] public bool ValueBool;   // 1
    [FieldOffset(4)] public int ValueInt32;   // 4 

    public bool IsNull => this.Type == BsonType.Null;
    public bool IsMinValue => this.Type == BsonType.MinValue;
    public bool IsMaxValue => this.Type == BsonType.MaxValue;

    /// <summary>
    /// Get how many bytes, in memory, this IndexKey are using
    /// </summary>
    public int IndexKeySize
    {
        get
        {
            var header = sizeof(IndexKey);
            var valueSize = this.Type switch
            {
                BsonType.Boolean => 0, // 0 (1 but use header hi-space)
                BsonType.Int32 => 0,   // 0 (4 but use header hi-space)
                _ => this.KeyLength
            };
            var padding = valueSize % 8 > 0 ? 8 - (valueSize % 8) : 0;

            return header + valueSize + padding;
        }
    }

    public override string ToString()
    {
        fixed(IndexKey* indexKey = &this)
        {
            var value = ToBsonValue(indexKey);

            return Dump.Object(new { Type, KeyLength, IndexKeySize, Value = value.ToString() });
        }
    }

    public static void CopyIndexKey(IndexKey* sourceIndexKey, IndexKey* targetIndexKey)
    {
        // get span from each page pointer
        var sourceSpan = new Span<byte>(sourceIndexKey, PAGE_SIZE);
        var targetSpan = new Span<byte>(targetIndexKey, PAGE_SIZE);

        sourceSpan.CopyTo(targetSpan);
    }
}
