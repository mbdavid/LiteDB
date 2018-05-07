using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public class BsonArray : BsonValue, IList<BsonValue>
    {
        public BsonArray()
            : base(new List<BsonValue>())
        {
        }

        public BsonArray(List<BsonValue> array)
            : base(array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
        }

        public BsonArray(BsonValue[] array)
            : base(new List<BsonValue>(array))
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
        }

        public BsonArray(IEnumerable<BsonValue> items)
            : this()
        {
            this.AddRange<BsonValue>(items);
        }

        public BsonArray(IEnumerable<BsonArray> items)
            : this()
        {
            this.AddRange<BsonArray>(items);
        }

        public BsonArray(IEnumerable<BsonDocument> items)
            : this()
        {
            this.AddRange<BsonDocument>(items);
        }

        public new List<BsonValue> RawValue
        {
            get
            {
                return (List<BsonValue>)base.RawValue;
            }
        }

        public BsonValue this[int index]
        {
            get
            {
                return this.RawValue.ElementAt(index);
            }
            set
            {
                this.RawValue[index] = value ?? BsonValue.Null;
            }
        }

        public int Count
        {
            get
            {
                return this.RawValue.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(BsonValue item)
        {
            this.RawValue.Add(item ?? BsonValue.Null);
        }

        public virtual void AddRange<T>(IEnumerable<T> array)
            where T : BsonValue
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            foreach (var item in array)
            {
                this.Add(item ?? BsonValue.Null);
            }
        }

        public void Clear()
        {
            this.RawValue.Clear();
        }

        public bool Contains(BsonValue item)
        {
            return this.RawValue.Contains(item);
        }

        public void CopyTo(BsonValue[] array, int arrayIndex)
        {
            this.RawValue.CopyTo(array, arrayIndex);
        }

        public IEnumerator<BsonValue> GetEnumerator()
        {
            return this.RawValue.GetEnumerator();
        }

        public int IndexOf(BsonValue item)
        {
            return this.RawValue.IndexOf(item);
        }

        public void Insert(int index, BsonValue item)
        {
            this.RawValue.Insert(index, item);
        }

        public bool Remove(BsonValue item)
        {
            return this.RawValue.Remove(item);
        }

        public void RemoveAt(int index)
        {
            this.RawValue.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var value in this.RawValue)
            {
                yield return new BsonValue(value);
            }
        }

        public override int CompareTo(BsonValue other)
        {
            // if types are different, returns sort type order
            if (other.Type != BsonType.Document) return this.Type.CompareTo(other.Type);

            var otherArray = other.AsArray;

            var result = 0;
            var i = 0;
            var stop = Math.Min(this.Count, otherArray.Count);

            // compare each element
            for (; 0 == result && i < stop; i++)
                result = this[i].CompareTo(otherArray[i]);

            if (result != 0) return result;
            if (i == this.Count) return i == otherArray.Count ? 0 : -1;
            return 1;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, false, true);
        }
    }
}