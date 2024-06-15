namespace LiteDB;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LiteDB.Engine;

public class BsonDocument : BsonValue, IDictionary<string, BsonValue>
{
    public BsonDocument()
        : base(BsonType.Document, new Dictionary<string, BsonValue>(StringComparer.OrdinalIgnoreCase))
    {
    }

    public BsonDocument(ConcurrentDictionary<string, BsonValue> dict)
        : this()
    {
        if (dict == null)
            throw new ArgumentNullException(nameof(dict));

        foreach (var element in dict)
        {
            Add(element);
        }
    }

    public BsonDocument(IDictionary<string, BsonValue> dict)
        : this()
    {
        if (dict == null)
            throw new ArgumentNullException(nameof(dict));

        foreach (var element in dict)
        {
            Add(element);
        }
    }

    public new IDictionary<string, BsonValue> RawValue => base.RawValue as IDictionary<string, BsonValue>;

    /// <summary>
    ///     Get/Set position of this document inside database. It's filled when used in Find operation.
    /// </summary>
    internal PageAddress RawId { get; set; } = PageAddress.Empty;

    /// <summary>
    ///     Get/Set a field for document. Fields are case sensitive
    /// </summary>
    public override BsonValue this[string key]
    {
        get { return RawValue.GetOrDefault(key, Null); }
        set { RawValue[key] = value ?? Null; }
    }

    #region CompareTo

    public override int CompareTo(BsonValue other)
    {
        // if types are different, returns sort type order
        if (other.Type != BsonType.Document)
            return Type.CompareTo(other.Type);

        var thisKeys = Keys.ToArray();
        var thisLength = thisKeys.Length;

        var otherDoc = other.AsDocument;
        var otherKeys = otherDoc.Keys.ToArray();
        var otherLength = otherKeys.Length;

        var result = 0;
        var i = 0;
        var stop = Math.Min(thisLength, otherLength);

        for (; 0 == result && i < stop; i++)
            result = this[thisKeys[i]].CompareTo(otherDoc[thisKeys[i]]);

        // are different
        if (result != 0)
            return result;

        // test keys length to check which is bigger
        if (i == thisLength)
            return i == otherLength ? 0 : -1;

        return 1;
    }

    #endregion

    #region IDictionary

    public ICollection<string> Keys => RawValue.Keys;

    public ICollection<BsonValue> Values => RawValue.Values;

    public int Count => RawValue.Count;

    public bool IsReadOnly => false;

    public bool ContainsKey(string key) => RawValue.ContainsKey(key);

    /// <summary>
    ///     Get all document elements - Return "_id" as first of all (if exists)
    /// </summary>
    public IEnumerable<KeyValuePair<string, BsonValue>> GetElements()
    {
        if (RawValue.TryGetValue("_id", out var id))
        {
            yield return new KeyValuePair<string, BsonValue>("_id", id);
        }

        foreach (var item in RawValue.Where(x => x.Key != "_id"))
        {
            yield return item;
        }
    }

    public void Add(string key, BsonValue value) => RawValue.Add(key, value ?? Null);

    public bool Remove(string key) => RawValue.Remove(key);

    public void Clear() => RawValue.Clear();

    public bool TryGetValue(string key, out BsonValue value) => RawValue.TryGetValue(key, out value);

    public void Add(KeyValuePair<string, BsonValue> item) => Add(item.Key, item.Value);

    public bool Contains(KeyValuePair<string, BsonValue> item) => RawValue.Contains(item);

    public bool Remove(KeyValuePair<string, BsonValue> item) => Remove(item.Key);

    public IEnumerator<KeyValuePair<string, BsonValue>> GetEnumerator() => RawValue.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => RawValue.GetEnumerator();

    public void CopyTo(KeyValuePair<string, BsonValue>[] array, int arrayIndex)
    {
        RawValue.CopyTo(array, arrayIndex);
    }

    public void CopyTo(BsonDocument other)
    {
        foreach (var element in this)
        {
            other[element.Key] = element.Value;
        }
    }

    #endregion

    private int _length;

    internal override int GetBytesCount(bool recalc)
    {
        if (recalc == false && _length > 0)
            return _length;

        var length = 5;

        foreach (var element in RawValue)
        {
            length += GetBytesCountElement(element.Key, element.Value);
        }

        return _length = length;
    }
}