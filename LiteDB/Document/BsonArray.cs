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

    public class BsonArray : BsonValue, IList<BsonValue>, IComparable<BsonValue>
    {
        private List<BsonValue> _items;

        public BsonArray()
        {
            _items = new List<BsonValue>();
            Type = BsonType.Array;
            Length = 5;
            _arrayValue = this;
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
                Length -= GetBytesCountElement(_items[index]);
                _items[index].LengthChanged -= OnLengthChanged;

                var item = value ?? BsonValue.Null;

                _items[index] = item;

                if (item.IsArray || item.IsDocument)
                    item.LengthChanged += OnLengthChanged;

                Length += GetBytesCountElement(item);
            }
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;


        public void Add(BsonValue item)
        {
            item = item ?? BsonValue.Null;
            _items.Add(item);

            if (item.IsArray || item.IsDocument)
                item.LengthChanged += OnLengthChanged;

            Length += GetBytesCountElement(item);
        }

        public void AddRange<T>(IEnumerable<T> array) where T : BsonValue
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            foreach (var item in array)
                Add(item);
        }

        public void Clear()
        {
            _items.Clear();

            foreach (var item in _items)
                item.LengthChanged -= OnLengthChanged;

            Length = 5;
        }

        public bool Contains(BsonValue item) => _items.Contains(item);

        public void CopyTo(BsonValue[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public IEnumerator<BsonValue> GetEnumerator() => _items.GetEnumerator();

        public int IndexOf(BsonValue item) => _items.IndexOf(item);

        public void Insert(int index, BsonValue item)
        {
            item = item ?? BsonValue.Null;
            _items.Insert(index, item);

            if (item.IsArray || item.IsDocument)
                item.LengthChanged += OnLengthChanged;

            Length += GetBytesCountElement(item);
        }

        public bool Remove(BsonValue item)
        {
            Length -= item.Length;
            item.LengthChanged -= OnLengthChanged;
            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            var item = _items[index];
            item.LengthChanged -= OnLengthChanged;
            Length -= item.Length;
            _items.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override int CompareTo(BsonValue other)
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

        public override string ToString() => JsonSerializer.Serialize(this);

        public override bool Equals(object obj) => CompareTo(new BsonValue(obj)) == 0;

        public override int GetHashCode() => _items.GetHashCode();

        #region Length

        internal override int Length
        {
            get => _length;
            set
            {
                if (_length != value)
                {
                    var difference = value - _length;
                    _length = value;
                    NotifyLengthChanged(difference);
                }
            }
        }
        private int _length;

        private int GetBytesCountElement(BsonValue value) => 1 + value.Length;

        private void OnLengthChanged(object sender, int e) => Length += e;

        #endregion
    }
}
