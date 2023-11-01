namespace LiteDB.Engine;

unsafe internal partial struct IndexKey
{
    #region Compare IndexKey* vs IndexKey*

    public static int Compare(IndexKey* left, IndexKey* right, Collation collation)
    {
        // first, test if types are different
        if (left->Type != right->Type)
        {
            // check with both sides are number (in diferent data types, but can be cast)
            if (left->Type.IsNumeric() && right->Type.IsNumeric())
            {
                return CompareNumbers(left, right);
            }
            // if not, order by sort type order
            else
            {
                var result = left->Type.CompareTo(right->Type);
                return result < 0 ? -1 : result > 0 ? +1 : 0;
            }
        }

        var leftValuePtr = (nint)left + sizeof(IndexKey);
        var rightValuePtr = (nint)right + sizeof(IndexKey);

        switch (left->Type)
        {
            case BsonType.MinValue:
            case BsonType.MaxValue:
            case BsonType.Null: 
                return 0;
            case BsonType.Boolean: return left->ValueBool.CompareTo(right->ValueBool);
            case BsonType.Int32: return left->ValueInt32.CompareTo(right->ValueInt32);

            case BsonType.Int64: return (*(long*)leftValuePtr).CompareTo(*(long*)rightValuePtr);
            case BsonType.Double: return (*(double*)leftValuePtr).CompareTo(*(double*)rightValuePtr);
            case BsonType.DateTime: return (*(DateTime*)leftValuePtr).CompareTo(*(DateTime*)rightValuePtr);

            case BsonType.ObjectId: return (*(ObjectId*)leftValuePtr).CompareTo(*(ObjectId*)rightValuePtr);
            case BsonType.Guid: return (*(Guid*)leftValuePtr).CompareTo(*(Guid*)rightValuePtr);
            case BsonType.Decimal: return (*(decimal*)leftValuePtr).CompareTo(*(decimal*)rightValuePtr);

            case BsonType.String:
                var leftString = Encoding.UTF8.GetString((byte*)leftValuePtr, left->KeyLength);
                var rightString = Encoding.UTF8.GetString((byte*)rightValuePtr, right->KeyLength);

                return collation.Compare(leftString, rightString);

            case BsonType.Binary:
                var leftBinary = new Span<byte>((byte*)leftValuePtr, left->KeyLength);
                var rightBinary = new Span<byte>((byte*)rightValuePtr, right->KeyLength);                

                return leftBinary.SequenceCompareTo(rightBinary);
        }

        throw new NotSupportedException();
    }

    /// <summary>
    /// Compare two different numbers types [bool, int32, int64, double, decimal]
    /// </summary>
    private static int CompareNumbers(IndexKey* left, IndexKey* right)
    {
        var leftValuePtr = (nint)left + sizeof(IndexNode);
        var rightValuePtr = (nint)right + sizeof(IndexNode);

        return (left->Type, right->Type) switch
        {
            (BsonType.Boolean, BsonType.Int32) => left->ValueBool.CompareTo(right->ValueInt32),
            (BsonType.Boolean, BsonType.Int64) => left->ValueBool.CompareTo(*(long*)rightValuePtr),
            (BsonType.Boolean, BsonType.Double) => left->ValueBool.CompareTo(*(double*)rightValuePtr),
            (BsonType.Boolean, BsonType.Decimal) => left->ValueBool.CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Int32, BsonType.Boolean) => left->ValueInt32.CompareTo(right->ValueBool),
            (BsonType.Int32, BsonType.Int64) => left->ValueInt32.CompareTo(*(long*)rightValuePtr),
            (BsonType.Int32, BsonType.Double) => left->ValueInt32.CompareTo(*(double*)rightValuePtr),
            (BsonType.Int32, BsonType.Decimal) => left->ValueInt32.CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Int64, BsonType.Boolean) => (*(long*)leftValuePtr).CompareTo(right->ValueBool),
            (BsonType.Int64, BsonType.Int32) => (*(long*)leftValuePtr).CompareTo(right->ValueInt32),
            (BsonType.Int64, BsonType.Double) => (*(long*)leftValuePtr).CompareTo(*(double*)rightValuePtr),
            (BsonType.Int64, BsonType.Decimal) => (*(long*)leftValuePtr).CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Double, BsonType.Boolean) => (*(double*)leftValuePtr).CompareTo(right->ValueBool),
            (BsonType.Double, BsonType.Int32) => (*(double*)leftValuePtr).CompareTo(right->ValueInt32),
            (BsonType.Double, BsonType.Int64) => (*(double*)leftValuePtr).CompareTo(*(long*)rightValuePtr),
            (BsonType.Double, BsonType.Decimal) => (*(double*)leftValuePtr).CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Decimal, BsonType.Boolean) => (*(decimal*)leftValuePtr).CompareTo(right->ValueBool),
            (BsonType.Decimal, BsonType.Int32) => (*(decimal*)leftValuePtr).CompareTo(right->ValueInt32),
            (BsonType.Decimal, BsonType.Int64) => (*(decimal*)leftValuePtr).CompareTo(*(long*)rightValuePtr),
            (BsonType.Decimal, BsonType.Double) => (*(decimal*)leftValuePtr).CompareTo(*(double*)rightValuePtr),

            _ => throw new NotImplementedException()
        };
    }

    #endregion

    #region Compare BsonValue vs IndexKey*

    internal static int Compare(BsonValue left, IndexKey* right, Collation collation)
    {
        // first, test if types are different
        if (left.Type != right->Type)
        {
            // check with both sides are number (in diferent data types, but can be cast)
            if (left.Type.IsNumeric() && right->Type.IsNumeric())
            {
                return CompareNumbers(left, right);
            }
            // if not, order by sort type order
            else
            {
                var result = left.Type.CompareTo(right->Type);
                return result < 0 ? -1 : result > 0 ? +1 : 0;
            }
        }

        var rightValuePtr = (nint)right + sizeof(IndexKey);

        switch (left.Type)
        {
            case BsonType.MinValue:
            case BsonType.MaxValue:
            case BsonType.Null:
                return 0;
            case BsonType.Boolean: return left.AsBoolean.CompareTo(right->ValueBool);
            case BsonType.Int32: return left.AsInt32.CompareTo(right->ValueInt32);

            case BsonType.Int64: return left.AsInt64.CompareTo(*(long*)rightValuePtr);
            case BsonType.Double: return left.AsDouble.CompareTo(*(double*)rightValuePtr);
            case BsonType.DateTime: return left.AsDateTime.CompareTo(*(DateTime*)rightValuePtr);

            case BsonType.ObjectId: return left.AsObjectId.CompareTo(*(ObjectId*)rightValuePtr);
            case BsonType.Guid: return left.AsGuid.CompareTo(*(Guid*)rightValuePtr);
            case BsonType.Decimal: return left.AsDecimal.CompareTo(*(decimal*)rightValuePtr);

            case BsonType.String:
                var rightString = Encoding.UTF8.GetString((byte*)rightValuePtr, right->KeyLength);

                return collation.Compare(left.AsString, rightString);

            case BsonType.Binary:
                var rightBinary = new Span<byte>((byte*)rightValuePtr, right->KeyLength);

                return left.AsBinary.AsSpan().SequenceCompareTo(rightBinary);
        }

        throw new NotSupportedException();
    }

    /// <summary>
    /// Compare two different numbers types [bool, int32, int64, double, decimal]
    /// </summary>
    private static int CompareNumbers(BsonValue left, IndexKey* right)
    {
        var rightValuePtr = (nint)right + sizeof(IndexNode);

        return (left.Type, right->Type) switch
        {
            (BsonType.Boolean, BsonType.Int32) => left.AsBoolean.CompareTo(right->ValueInt32),
            (BsonType.Boolean, BsonType.Int64) => left.AsBoolean.CompareTo(*(long*)rightValuePtr),
            (BsonType.Boolean, BsonType.Double) => left.AsBoolean.CompareTo(*(double*)rightValuePtr),
            (BsonType.Boolean, BsonType.Decimal) => left.AsBoolean.CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Int32, BsonType.Boolean) => left.AsInt32.CompareTo(right->ValueBool),
            (BsonType.Int32, BsonType.Int64) => left.AsInt32.CompareTo(*(long*)rightValuePtr),
            (BsonType.Int32, BsonType.Double) => left.AsInt32.CompareTo(*(double*)rightValuePtr),
            (BsonType.Int32, BsonType.Decimal) => left.AsInt32.CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Int64, BsonType.Boolean) => left.AsInt64.CompareTo(right->ValueBool),
            (BsonType.Int64, BsonType.Int32) => left.AsInt64.CompareTo(right->ValueInt32),
            (BsonType.Int64, BsonType.Double) => left.AsInt64.CompareTo(*(double*)rightValuePtr),
            (BsonType.Int64, BsonType.Decimal) => left.AsInt64.CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Double, BsonType.Boolean) => left.AsDouble.CompareTo(right->ValueBool),
            (BsonType.Double, BsonType.Int32) => left.AsDouble.CompareTo(right->ValueInt32),
            (BsonType.Double, BsonType.Int64) => left.AsDouble.CompareTo(*(long*)rightValuePtr),
            (BsonType.Double, BsonType.Decimal) => left.AsDouble.CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Decimal, BsonType.Boolean) => left.AsDecimal.CompareTo(right->ValueBool),
            (BsonType.Decimal, BsonType.Int32) => left.AsDecimal.CompareTo(right->ValueInt32),
            (BsonType.Decimal, BsonType.Int64) => left.AsDecimal.CompareTo(*(long*)rightValuePtr),
            (BsonType.Decimal, BsonType.Double) => left.AsDecimal.CompareTo(*(double*)rightValuePtr),

            _ => throw new NotImplementedException()
        };
    }

    internal static int Compare(IndexKey* left, BsonValue right, Collation collation)
        => Compare(right, left, collation) * -1;

    #endregion
}
