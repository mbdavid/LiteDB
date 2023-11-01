namespace LiteDB.Engine;

unsafe internal partial struct IndexKey
{
    public static BsonValue ToBsonValue(IndexKey* indexKey)
    {
        var ptr = (nint)indexKey + sizeof(long);

        return indexKey->Type switch
        {
            BsonType.MinValue => BsonValue.MinValue,
            BsonType.MaxValue => BsonValue.MaxValue,
            BsonType.Null => BsonValue.Null,

            BsonType.Boolean => indexKey->ValueBool,
            BsonType.Int32 => indexKey->ValueInt32,

            BsonType.Int64 => *(long*)ptr,
            BsonType.Double => *(double*)ptr,
            BsonType.DateTime => *(DateTime*)ptr,

            BsonType.ObjectId => *(ObjectId*)ptr,
            BsonType.Guid => *(Guid*)ptr,
            BsonType.Decimal => *(decimal*)ptr,

            BsonType.String => Encoding.UTF8.GetString((byte*)ptr, indexKey->KeyLength),
            BsonType.Binary => new Span<byte>((byte*)ptr, indexKey->KeyLength).ToArray(),

            _ => throw new NotSupportedException()
        };

    }
}
