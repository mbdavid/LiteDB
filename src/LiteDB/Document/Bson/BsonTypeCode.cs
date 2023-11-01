namespace LiteDB;

/// <summary>
/// Represent BSON data type in disk only (used when serialize/deserialize). 
/// Do not change values in current FILE_VERSION (can be added)
/// Must use 5 bits only (3 bis are reserved)
/// </summary>
public enum BsonTypeCode : byte
{
    MinValue = 0,

    Null = 1,

    Int32 = 2,
    Int64 = 3,
    Double = 4,
    Decimal = 5,

    String = 6,

    Document = 7,
    Array = 8,

    Binary = 9,
    ObjectId = 10,
    Guid = 11,
    DateTime = 12,
    // reserved for DateTimeOffset
    False = 20, // Boolean False
    True = 21, // Boolean True

    MaxValue = 31, // use 5 bits max (keep 3 for extends)

}