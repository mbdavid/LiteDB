namespace LiteDB;

/// <summary>
/// Represent a array of BsonValue in Bson object model
/// </summary>
public class BsonArray : BsonValue, IList<BsonValue>
{
    /// <summary>
    /// Singleton Empty BsonArray (readonly)
    /// </summary>
    public static readonly BsonArray Empty = Array.Empty<BsonArray>();

    private readonly IList<BsonValue> _list;

    private int _length = -1;
    private bool _readonly = false;

    /// <summary>
    /// Get internal list of items (can be from an IList or IReadOnlyList)
    /// </summary>
    public IList<BsonValue> Value => _list;

    public BsonArray() : this(0)
    {
    }

    public BsonArray(int capacity)
    {
        _list = new List<BsonValue>(capacity);
    }

    public BsonArray(IEnumerable<BsonValue> values)
    {
        if (values is IList<BsonValue> list)
        {
            _readonly = values.GetType().IsArray;

            _list = list;
        }
        else
        {
            _readonly = false;

            _list = new List<BsonValue>(values);
        }
    }

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

    #region Implicit Ctor

    public static implicit operator BsonArray(List<BsonValue> value) => new ((IList<BsonValue>)value);

    public static implicit operator BsonArray(BsonValue[] value) => new ((IReadOnlyList<BsonValue>)value);

    #endregion

    #region IList implementation

    public override BsonValue this[int index]
    {
        get => _list[index];
        set
        {
            if (_readonly) throw ERR_READONLY_OBJECT();

            _list[index] = value;
        }
    }

    public void Add(BsonValue item)
    {
        if (_readonly) throw ERR_READONLY_OBJECT();

        _list.Add(item ?? BsonValue.Null);
    }

    public void Clear()
    {
        if (_readonly) throw ERR_READONLY_OBJECT();

        _list.Clear();
    }

    public bool Remove(BsonValue item)
    {
        if (_readonly) throw ERR_READONLY_OBJECT();

        return _list.Remove(item ?? BsonValue.Null);
    }

    public void RemoveAt(int index)
    {
        if (_readonly) throw ERR_READONLY_OBJECT();

        _list.RemoveAt(index);
    }

    public void Insert(int index, BsonValue item)
    {
        if (_readonly) throw ERR_READONLY_OBJECT();

        _list.Insert(index, item ?? BsonValue.Null);
    }

    public int Count => this.Value.Count;

    public bool IsReadOnly => _readonly;

    public int IndexOf(BsonValue item) => _list.IndexOf(item);

    public bool Contains(BsonValue item) =>  _list.Contains(item);

    public bool Contains(BsonValue item, Collation collation) => _list.Contains(item);

    public void CopyTo(BsonValue[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

    public IEnumerator<BsonValue> GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

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
