using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LiteDB
{
    public class BsonArray : BsonValue, IEnumerable<BsonValue>
    {
        public BsonArray()
            : base(new List<BsonValue>())
        {
        }

        public BsonArray(List<BsonValue> array)
            : base(array)
        {
        }

        public BsonArray(IEnumerable<BsonValue> items)
            : this()
        {
            this.AddRange<BsonValue>(items);
        }

        public BsonArray(IEnumerable<BsonObject> items)
            : this()
        {
            this.AddRange<BsonObject>(items);
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

        public BsonValue this[int index]
        {
            get
            {
                return this.RawValue.ElementAt(index);
            }
            set
            {
                this.RawValue[index] = value == null ? BsonValue.Null : value;
            }
        }

        public void Add(BsonValue value)
        {
            if (value == null) value = BsonValue.Null;

            this.RawValue.Add(value);
        }

        public void AddRange<T>(IEnumerable<T> array)
            where T : BsonValue
        {
            foreach (var item in array)
            {
                this.Add(item);
            }
        }

        public void Remove(int index)
        {
            this.RawValue.RemoveAt(index);
        }

        public int Count
        {
            get
            {
                return this.RawValue.Count;
            }
        }

        public new List<BsonValue> RawValue
        {
            get
            {
                return (List<BsonValue>)base.RawValue;
            }
        }

        public IEnumerator<BsonValue> GetEnumerator()
        {
            foreach (var value in this.RawValue)
            {
                yield return new BsonValue(value);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

    }
}
