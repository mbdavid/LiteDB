using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB
{
    public static class BsonArrayExtensions
    {
        /// <summary>
        /// Convert an IEnumerable of BsonValues into a single BsonArray with all elements
        /// </summary>
        public static BsonArray ToBsonArray(this IEnumerable<BsonValue> values)
        {
            return new BsonArray(values);
        }
    }

    public class BsonArray : IList<BsonValue>, IComparable<BsonValue>
    {
        private List<BsonValue> _items;

        public BsonArray()
        {
            _items = new List<BsonValue>();
            Length = 5;
        }

        public BsonArray(List<BsonValue> array) : this()
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            AddRange(array);
        }

        public BsonArray(BsonValue[] array) : this(array.ToList()) { }

        public BsonArray(IEnumerable<BsonValue> items) : this()
        {
            AddRange(items);
        }

        public BsonValue this[int index]
        {
            get => _items[index];
            set
            {
                Length -= GetBytesCountElement(index.ToString(), _items[index]);
                _items[index] = value ?? BsonValue.Null;
                Length += GetBytesCountElement(index.ToString(), value);
            }
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;


        public void Add(BsonValue item)
        {
            _items.Add(item ?? BsonValue.Null);
            Length += GetBytesCountElement((_items.Count - 1).ToString(), item);
        }

        public virtual void AddRange<T>(IEnumerable<T> array) where T : BsonValue
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            foreach (var item in array)
                Add(item ?? BsonValue.Null);
        }

        public void Clear()
        {
            _items.Clear();
            Length = 5;
        }

        public bool Contains(BsonValue item) => _items.Contains(item);

        public void CopyTo(BsonValue[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public IEnumerator<BsonValue> GetEnumerator() => _items.GetEnumerator();

        public int IndexOf(BsonValue item) => _items.IndexOf(item);

        public void Insert(int index, BsonValue item)
        {
            _items.Insert(index, item);
            Length += GetBytesCountElement((_items.Count - 1).ToString(), item);
        }

        public bool Remove(BsonValue item)
        {
            //Length -= item.Length;
            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            //Length -= _items[index].Length;
            _items.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int CompareTo(BsonValue other)
        {
            // if types are different, returns sort type order
            if (other.Type != BsonType.Array)
                return BsonType.Array.CompareTo(other.Type);

            var otherArray = other.AsArray;

            var result = 0;
            var i = 0;
            var stop = Math.Min(Count, otherArray.Count);

            // compare each element
            for (; 0 == result && i < stop; i++)
                result = this[i].CompareTo(otherArray[i]);

            if (result != 0) return result;
            if (i == this.Count) return i == otherArray.Count ? 0 : -1;
            return 1;
        }

        public static implicit operator List<BsonValue>(BsonArray value) => value._items;

        public override bool Equals(object obj) => CompareTo(new BsonValue(obj)) == 0;

        public override int GetHashCode() => _items.GetHashCode();

        public int Length;

        private int GetBytesCountElement(string key, BsonValue value)
        {
            return
                1 + // element type
                value?.Length ?? 0 +
                (value.Type == BsonType.String || value.Type == BsonType.Binary || value.Type == BsonType.Guid ? 5 : 0); // bytes.Length + 0x??
        }
    }
}
