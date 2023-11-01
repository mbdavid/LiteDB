namespace LiteDB;

internal partial class BsonExpressionMethods
{
    /// <summary>
    /// Parse a JSON string into a new BsonValue
    /// JSON('{a:1}') = {a:1}
    /// </summary>
    public static BsonValue JSON(BsonValue str)
    {
        if (str is not BsonString json) return BsonValue.Null;

        BsonValue? result = null;

        try
        {
            result = JsonSerializer.Deserialize(json.AsString);
        }
        catch (LiteException ex) when (ex.ErrorCode == 0 /*LiteException.UNEXPECTED_TOKEN*/)
        {
        }

        return result ?? BsonValue.Null;
    }

    /// <summary>
    /// Create a new document and copy all properties from source document. Then copy properties (overritting if need) extend document
    /// Always returns a new document!
    /// EXTEND($, {a: 2}) = {_id:1, a: 2}
    /// </summary>
    public static BsonValue EXTEND(BsonValue source, BsonValue extend)
    {
        if (source is BsonDocument src && extend is BsonDocument ext)
        {
            throw new NotImplementedException();
        }
        else if (source.IsDocument) return source;
        else if (extend.IsDocument) return extend;

        return BsonDocument.Empty;
    }

    /// <summary>
    /// Get all KEYS names from a document
    /// </summary>
    public static BsonValue KEYS(BsonValue document)
    {
        if (document is not BsonDocument doc) return BsonArray.Empty;

        var keys = new BsonArray();

        foreach (var key in doc.Keys)
        {
            keys.Add(key);
        }

        return keys;
    }

    /// <summary>
    /// Get all values from a document
    /// </summary>
    public static BsonValue VALUES(BsonValue document)
    {
        if (document is not BsonDocument doc) return BsonArray.Empty;

        var array = new BsonArray();

        foreach (var item in doc.Values)
        {
            array.Add(item);
        }

        return array;
    }

    /// <summary>
    /// Return CreationTime from ObjectId value - returns null if not an ObjectId
    /// </summary>
    public static BsonValue OID_CREATIONTIME(BsonValue objectID)
    {
        if (objectID is not BsonObjectId objId) return BsonValue.Null;

        return objId.Value.CreationTime;
    }

    /// <summary>
    /// Conditional IF statment. If condition are true, returns TRUE value, otherwise, FALSE value
    /// </summary>
    public static BsonValue IIF(BsonValue test, BsonValue ifTrue, BsonValue ifFalse)
    {
        // this method are not implemented because will use "Expression.Conditional"
        // will execute "ifTrue" only if test = true and will execute "ifFalse" if test = false
        throw new NotImplementedException();
    }

    /// <summary>
    /// Return first values if not null. If null, returns second value.
    /// </summary>
    public static BsonValue COALESCE(BsonValue left, BsonValue right)
    {
        return left.IsNull ? right : left;
    }

    /// <summary>
    /// Return length of variant value (valid only for String, Binary, Array or Document [keys])
    /// </summary>
    public static BsonValue LENGTH(BsonValue value)
    {
        if (value is BsonString str) return str.Value.Length;
        if (value is BsonBinary bin) return bin.Value.Length;
        if (value is BsonArray arr) return arr.Count;
        if (value is BsonDocument doc) return doc.Keys.Count;

        return 0;
    }

    /// <summary>
    /// Returns the first num elements of values.
    /// </summary>
    public static BsonValue TOP(BsonValue values, BsonValue num)
    {
        if (values is not BsonArray arr) return BsonArray.Empty;
        if (num is not BsonInt32 index) return BsonArray.Empty;

        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the union of the two enumerables.
    /// </summary>
    public static BsonValue UNION(BsonValue left, BsonValue right)
    {
        if (left is not BsonArray arrLeft) return BsonArray.Empty;
        if (right is not BsonInt32 arrRight) return BsonArray.Empty;

        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the set difference between the two enumerables.
    /// </summary>
    public static BsonValue EXCEPT(BsonValue left, BsonValue right)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a unique list of items
    /// </summary>
    public static IEnumerable<BsonValue> DISTINCT(BsonValue values)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Return a random int value
    /// </summary>
    [Volatile]
    public static BsonValue RANDOM()
    {
        return Randomizer.Next();
    }

    /// <summary>
    /// Return a ranom int value inside this min/max values
    /// </summary>
    [Volatile]
    public static BsonValue RANDOM(BsonValue min, BsonValue max)
    {
        if (min is BsonInt32 bmin && max is BsonInt32 bmax)
        {
            return Randomizer.Next(bmin, bmax);
        }
        else
        {
            return BsonValue.Null;
        }
    }
}
