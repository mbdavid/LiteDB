namespace LiteDB.Engine;

unsafe internal partial struct IndexKey
{
    /// <summary>
    /// Get how many bytes IndexKey structure will need to represent this BsonValue (should be padded)
    /// </summary>
    public static int GetSize(BsonValue value, out int valueSize)
    {
        var maxKeyLength = MAX_INDEX_KEY_SIZE - sizeof(IndexKey);

        valueSize = value.Type switch
        {
            BsonType.MaxValue => 0,
            BsonType.MinValue => 0,
            BsonType.Null => 0,
            BsonType.Boolean => 0, // use indexKey header hi 4-bytes
            BsonType.Int32 => 0, // use indexKey header hi 4-bytes
            BsonType.Int64 => sizeof(long), // 8
            BsonType.Double => sizeof(double), // 8
            BsonType.DateTime => sizeof(DateTime), // 8
            BsonType.ObjectId => sizeof(ObjectId), // 12
            BsonType.Decimal => sizeof(decimal), // 16
            BsonType.Guid => sizeof(Guid), // 16
            BsonType.String => Encoding.UTF8.GetByteCount(value.AsString),
            BsonType.Binary => value.AsBinary.Length,
            _ => throw ERR($"This object type `{value.Type}` are not supported as an index key")
        };

        if (valueSize > maxKeyLength) throw ERR($"index value too excedded {maxKeyLength}");

        var header = sizeof(IndexKey);
        var padding = valueSize % 8 > 0 ? 8 - (valueSize % 8) : 0;
        var result = header + valueSize + padding;

        return result;
        //Console.WriteLine(Dump.Object(new { Type = value.Type, header, valueSize, padding, result }));
    }
}
