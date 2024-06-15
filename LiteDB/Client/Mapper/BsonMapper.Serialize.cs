namespace LiteDB;

using System;
using System.Collections;
using System.Linq;
using System.Reflection;

public partial class BsonMapper
{
    /// <summary>
    ///     Serialize a entity class to BsonDocument
    /// </summary>
    public virtual BsonDocument ToDocument(Type type, object entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // if object is BsonDocument, just return them
        if (entity is BsonDocument)
            return (BsonDocument) entity;

        return Serialize(type, entity, 0).AsDocument;
    }

    /// <summary>
    ///     Serialize a entity class to BsonDocument
    /// </summary>
    public virtual BsonDocument ToDocument<T>(T entity)
    {
        return ToDocument(typeof(T), entity)?.AsDocument;
    }

    /// <summary>
    ///     Serialize to BsonValue any .NET object based on T type (using mapping rules)
    /// </summary>
    public BsonValue Serialize<T>(T obj)
    {
        return Serialize(typeof(T), obj, 0);
    }

    /// <summary>
    ///     Serialize to BsonValue any .NET object based on type parameter (using mapping rules)
    /// </summary>
    public BsonValue Serialize(Type type, object obj)
    {
        return Serialize(type, obj, 0);
    }

    internal BsonValue Serialize(Type type, object obj, int depth)
    {
        if (++depth > MaxDepth)
            throw LiteException.DocumentMaxDepth(MaxDepth, type);

        if (obj == null)
            return BsonValue.Null;

        // if is already a bson value
        if (obj is BsonValue bsonValue)
            return bsonValue;

        // check if is a custom type
        if (_customSerializer.TryGetValue(type, out var custom) ||
            _customSerializer.TryGetValue(obj.GetType(), out custom))
        {
            return custom(obj);
        }
        // test string - mapper has some special options

        if (obj is String)
        {
            var str = TrimWhitespace ? (obj as String).Trim() : (String) obj;

            if (EmptyStringToNull && str.Length == 0)
            {
                return BsonValue.Null;
            }

            return new BsonValue(str);
        }
        // basic Bson data types (cast datatype for better performance optimization)

        if (obj is Int32)
            return new BsonValue((Int32) obj);
        if (obj is Int64)
            return new BsonValue((Int64) obj);
        if (obj is Double)
            return new BsonValue((Double) obj);
        if (obj is Decimal)
            return new BsonValue((Decimal) obj);
        if (obj is Byte[])
            return new BsonValue((Byte[]) obj);
        if (obj is ObjectId)
            return new BsonValue((ObjectId) obj);
        if (obj is Guid)
            return new BsonValue((Guid) obj);
        if (obj is Boolean)
            return new BsonValue((Boolean) obj);
        if (obj is DateTime)
            return new BsonValue((DateTime) obj);
        // basic .net type to convert to bson
        if (obj is Int16 || obj is UInt16 || obj is Byte || obj is SByte)
        {
            return new BsonValue(Convert.ToInt32(obj));
        }

        if (obj is UInt32)
        {
            return new BsonValue(Convert.ToInt64(obj));
        }

        if (obj is UInt64)
        {
            var ulng = ((UInt64) obj);
            var lng = unchecked((Int64) ulng);

            return new BsonValue(lng);
        }

        if (obj is Single)
        {
            return new BsonValue(Convert.ToDouble(obj));
        }

        if (obj is Char)
        {
            return new BsonValue(obj.ToString());
        }

        if (obj is Enum)
        {
            if (EnumAsInteger)
            {
                return new BsonValue((int) obj);
            }

            return new BsonValue(obj.ToString());
        }
        // for dictionary

        if (obj is IDictionary dict)
        {
            // when you are converting Dictionary<string, object>
            if (type == typeof(object))
            {
                type = obj.GetType();
            }

            var itemType = type.GetTypeInfo().IsGenericType ? type.GetGenericArguments()[1] : typeof(object);

            return SerializeDictionary(itemType, dict, depth);
        }
        // check if is a list or array

        if (obj is IEnumerable)
        {
            return SerializeArray(Reflection.GetListItemType(type), obj as IEnumerable, depth);
        }
        // otherwise serialize as a plain object

        return SerializeObject(type, obj, depth);
    }

    private BsonArray SerializeArray(Type type, IEnumerable array, int depth)
    {
        var arr = new BsonArray();

        foreach (var item in array)
        {
            arr.Add(Serialize(type, item, depth));
        }

        return arr;
    }

    private BsonDocument SerializeDictionary(Type type, IDictionary dict, int depth)
    {
        var o = new BsonDocument();

        foreach (var key in dict.Keys)
        {
            var value = dict[key];
            var skey = key.ToString();

            if (key is DateTime dateKey)
            {
                skey = dateKey.ToString("o");
            }

            o[skey] = Serialize(type, value, depth);
        }

        return o;
    }

    private BsonDocument SerializeObject(Type type, object obj, int depth)
    {
        var t = obj.GetType();
        var doc = new BsonDocument();
        var entity = GetEntityMapper(t);

        // adding _type only where property Type is not same as object instance type
        if (type != t)
        {
            doc["_type"] = new BsonValue(_typeNameBinder.GetName(t));
        }

        foreach (var member in entity.Members.Where(x => x.Getter != null))
        {
            // get member value
            var value = member.Getter(obj);

            if (value == null && SerializeNullValues == false && member.FieldName != "_id")
                continue;

            // if member has a custom serialization, use it
            if (member.Serialize != null)
            {
                doc[member.FieldName] = member.Serialize(value, this);
            }
            else
            {
                doc[member.FieldName] = Serialize(member.DataType, value, depth);
            }
        }

        return doc;
    }
}