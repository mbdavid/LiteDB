namespace LiteDB.Engine;

internal class CollectionIndex
{
    /// <summary>
    ///     Slot index [0-255] used in all index nodes
    /// </summary>
    public byte Slot { get; }

    /// <summary>
    ///     Indicate index type: 0 = SkipList (reserved for future use)
    /// </summary>
    public byte IndexType { get; }

    /// <summary>
    ///     Index name
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Get index expression (path or expr)
    /// </summary>
    public string Expression { get; }

    /// <summary>
    ///     Get BsonExpression from Expression
    /// </summary>
    public BsonExpression BsonExpr { get; }

    /// <summary>
    ///     Indicate if this index has distinct values only
    /// </summary>
    public bool Unique { get; }

    /// <summary>
    ///     Head page address for this index
    /// </summary>
    public PageAddress Head { get; set; }

    /// <summary>
    ///     A link pointer to tail node
    /// </summary>
    public PageAddress Tail { get; set; }

    /// <summary>
    ///     Reserved byte (old max level)
    /// </summary>
    public byte Reserved { get; set; } = 1;

    /// <summary>
    ///     Free index page linked-list (all pages here must have at least 600 bytes)
    /// </summary>
    public uint FreeIndexPageList;

    /// <summary>
    ///     Returns if this index slot is empty and can be used as new index
    /// </summary>
    public bool IsEmpty
    {
        get { return string.IsNullOrEmpty(Name); }
    }

    public CollectionIndex(byte slot, byte indexType, string name, string expr, bool unique)
    {
        Slot = slot;
        IndexType = indexType;
        Name = name;
        Expression = expr;
        Unique = unique;
        FreeIndexPageList = uint.MaxValue;

        BsonExpr = BsonExpression.Create(expr);
    }

    public CollectionIndex(BufferReader reader)
    {
        Slot = reader.ReadByte();
        IndexType = reader.ReadByte();
        Name = reader.ReadCString();
        Expression = reader.ReadCString();
        Unique = reader.ReadBoolean();
        Head = reader.ReadPageAddress(); // 5
        Tail = reader.ReadPageAddress(); // 5
        Reserved = reader.ReadByte(); // 1
        FreeIndexPageList = reader.ReadUInt32(); // 4

        BsonExpr = BsonExpression.Create(Expression);
    }

    public void UpdateBuffer(BufferWriter writer)
    {
        writer.Write(Slot);
        writer.Write(IndexType);
        writer.WriteCString(Name);
        writer.WriteCString(Expression);
        writer.Write(Unique);
        writer.Write(Head);
        writer.Write(Tail);
        writer.Write(Reserved);
        writer.Write(FreeIndexPageList);
    }

    /// <summary>
    ///     Get index collection size used in CollectionPage
    /// </summary>
    public static int GetLength(CollectionIndex index)
    {
        return GetLength(index.Name, index.Expression);
    }

    /// <summary>
    ///     Get index collection size used in CollectionPage
    /// </summary>
    public static int GetLength(string name, string expr)
    {
        return
            1 + // Slot
            1 + // IndexType
            StringEncoding.UTF8.GetByteCount(name) +
            1 + // Name + \0
            StringEncoding.UTF8.GetByteCount(expr) +
            1 + // Expression + \0
            1 + // Unique
            PageAddress.SIZE + // Head
            PageAddress.SIZE + // Tail
            1 + // MaxLevel
            4; // FreeListPage
    }
}