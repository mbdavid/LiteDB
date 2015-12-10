namespace LiteDB
{
    /// <summary>
    /// All supported BsonTypes in sort order
    /// </summary>
    public enum BsonType
    {
        MinValue = 0,

        Null = 1,

        Int32 = 2,
        Int64 = 3,
        Double = 4,

        String = 5,

        Document = 6,
        Array = 7,

        Binary = 8,
        ObjectId = 9,
        Guid = 10,

        Boolean = 11,
        DateTime = 12,

        MaxValue = 13
    }
}