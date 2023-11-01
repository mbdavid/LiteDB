using System;

namespace LiteDB;

/// <summary>
/// Represent a array of BsonValue in Bson object model
/// </summary>
public class BsonArray : BsonValue, IList<BsonValue>
{
    /// <summary>
    /// Singleton Empty BsonArray (readonly)
    /// </summary>
    public static readonly BsonArray Empty = FromArray(Array.Empty<BsonArray>());

    private readonly IList<BsonValue>? _list;
    private readonly IReadOnlyList<BsonValue>? _array; // readonly list (when came from an array)

    private int _length = -1;

    /// <summary>
    /// Get internal list of items (can be from an IList or IReadOnlyList)
    /// </summary>
    public IReadOnlyList<BsonValue> Value =>
        _list is not null ? (IReadOnlyList<BsonValue>)_list : _array!;

    public BsonArray() : this(0)
    {
    }

    public BsonArray(int capacity)
    {
        _list = new List<BsonValue>(capacity);
        _array = null;
    }

    public BsonArray(IEnumerable<BsonValue> values)
        : this(0)
    {
        this.AddRange(values);
    }

    public BsonArray(IReadOnlyList<BsonValue> array)
        : this(0)
    {
        _list = null;
        _array = array;
    }

    #region Static constructors

    public BsonArray(IList<BsonValue>? list, IReadOnlyList<BsonValue>? array)
    {
        _list = list;
        _array = array;
    }

    /// <summary>
    /// Create a new instance of BsonArray using an already instance of IList (make BsonArray read/write)
    /// </summary>
    public static BsonArray FromList(IList<BsonValue> list)
        => new(list, null);

    /// <summary>
    /// Create a new instance of BsonArray using a new instance of List (make BsonArray read/write)
    /// </summary>
    public static BsonArray FromList(ICollection<BsonValue> collection)
        => new(new List<BsonValue>(collection), null);

    /// <summary>
    /// Create a new instance of BsonArray using an already instance of IReadOnlyList (make BsonArray as readonly)
    /// </summary>
    public static BsonArray FromArray(IReadOnlyList<BsonValue> array)
        => new(null, array);

    /// <summary>
    /// Create a new instance of BsonArray using an already instance of IReadOnlyList of BsonDocuments (make BsonArray as readonly)
    /// </summary>
    public static BsonArray FromArray(IReadOnlyList<BsonDocument> array)
        => new(null, array);

    #endregion

    public override BsonType Type => BsonType.Array;

    public override int GetBytesCount()
    {
        var length = sizeof(int); // for int32 length
        var count = this.Value.Count;

        for (var i = 0; i < count; i++)
        {
            length += GetBytesCountElement(this.Value[i]);
        }

        _length = length; // update local cache after loop

        return length;
    }

    internal override int GetBytesCountCached()
    {
        if (_length >= 0) return _length;

        return this.GetBytesCount();
    }

    public override int GetHashCode() => this.Value.GetHashCode();

    public void AddRange(IEnumerable<BsonValue> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        foreach (var item in items)
        {
            this.Add(item ?? BsonValue.Null);
        }
    }

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonArray otherArray)
        {
            // lhs and rhs might be subclasses of BsonArray
            using (var leftEnumerator = this.GetEnumerator())
            using (var rightEnumerator = otherArray.GetEnumerator())
            {
                while (true)
                {
                    var leftHasNext = leftEnumerator.MoveNext();
                    var rightHasNext = rightEnumerator.MoveNext();

                    if (!leftHasNext && !rightHasNext) return 0;
                    if (!leftHasNext) return -1;
                    if (!rightHasNext) return 1;

                    var leftValue = leftEnumerator.Current;
                    var rightValue = rightEnumerator.Current;

                    var result = leftValue.CompareTo(rightValue, collation);

                    if (result != 0) return result;
                }
            }
        }

        return this.CompareType(other);
    }

    #endregion

    #region IList implementation

    public override BsonValue this[int index]
    {
        get => this.Value[index];
        set
        {
            if (_list is null) throw ERR_READONLY_OBJECT();

            _list[index] = value;
        }
    }

    public void Add(BsonValue item)
    {
        if (_list is null) throw ERR_READONLY_OBJECT();

        _list.Add(item ?? BsonValue.Null);
    }

    public void Clear()
    {
        if (_list is null) throw ERR_READONLY_OBJECT();

        _list.Clear();
    }

    public bool Remove(BsonValue item)
    {
        if (_list is null) throw ERR_READONLY_OBJECT();

        return _list.Remove(item ?? BsonValue.Null);
    }

    public void RemoveAt(int index)
    {
        if (_list is null) throw ERR_READONLY_OBJECT();

        _list.RemoveAt(index);
    }

    public void Insert(int index, BsonValue item)
    {
        if (_list is null) throw ERR_READONLY_OBJECT();

        _list.Insert(index, item ?? BsonValue.Null);
    }

    public int Count => this.Value.Count;

    public bool IsReadOnly => _list is null;

    public int IndexOf(BsonValue item)
    {
        if (_list is not null) return _list.IndexOf(item);

        throw new NotImplementedException();
    }

    public bool Contains(BsonValue item)
    {
        if (_list is not null) return _list.Contains(item);

        throw new NotImplementedException();
    }

    public bool Contains(BsonValue item, Collation collation)
    {
        if (_list is not null)
        {
            return _list.Any(x => collation.Compare(x, item ?? BsonValue.Null) == 0);
        }

        throw new NotImplementedException();
    }

    public void CopyTo(BsonValue[] array, int arrayIndex)
    {
        if (_list is not null)
        {
            _list.CopyTo(array, arrayIndex);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public IEnumerator<BsonValue> GetEnumerator() => this.Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.Value.GetEnumerator();

    #endregion

    #region Static Helpers

    /// <summary>
    /// Get how many bytes one single element will used in BSON format
    /// </summary>
    internal static int GetBytesCountElement(BsonValue value)
    {
        // get data length
        var valueLength = value.GetBytesCountCached();

        // if data type is variant length, add int32 to length
        if (value.Type == BsonType.String ||
            value.Type == BsonType.Binary)
        {
            valueLength += sizeof(int);
        }

        return
            1 + // element value type
            valueLength;
    }

    #endregion
}
