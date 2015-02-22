using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal enum IndexDataType
    { 
        Null,
        Boolean,
        Int32,
        Int64,
        Double,
        String,
        DateTime,
        Guid
    };

    /// <summary>
    /// Represent a index key value - can be a string, int, decimal, guid, ... It's persistable
    /// </summary>
    internal struct IndexKey : IComparable<IndexKey>
    {
        public const int MAX_LENGTH_SIZE = 255;

        public readonly object Value;

        public readonly IndexDataType Type;

        public readonly bool IsNumber;

        public readonly int Length;

        public IndexKey(object value)
        {
            this.Value = value;
            this.IsNumber = false;

            if (value == null) { this.Type = IndexDataType.Null; this.Length = 0; }
            else if (value is Int32) { this.Type = IndexDataType.Int32; this.Length = 4; this.IsNumber = true; }
            else if (value is Int64) { this.Type = IndexDataType.Int64; this.Length = 8; this.IsNumber = true; }
            else if (value is Double) { this.Type = IndexDataType.Double; this.Length = 8; this.IsNumber = true; }
            else if (value is String) { this.Type = IndexDataType.String; this.Length = Encoding.UTF8.GetByteCount((string)Value) + 1 /* +1 = For String Length on store */; }
            else if (value is Boolean) { this.Type = IndexDataType.Boolean; this.Length = 1; }
            else if (value is DateTime) { this.Type = IndexDataType.DateTime; this.Length = 8; }
            else if (value is Guid) { this.Type = IndexDataType.Guid; this.Length = 16; }

            // if not found, exception
            else throw LiteException.IndexTypeNotSupport(value.GetType());

            // increase "Type" byte in length
            this.Length++;

            // withespace empty string == null
            if (this.Type == IndexDataType.String && ((string)value).Trim().Length == 0)
            {
                this.Value = null;
                this.Type = IndexDataType.Null;
                this.Length = 1;
            }

            // limit in 255 string bytes
            if (this.Type == IndexDataType.String && this.Length > MAX_LENGTH_SIZE)
            {
                throw LiteException.IndexKeyTooLong();
            }
        }

        public int CompareTo(IndexKey other)
        {
            // first, compare Null values (null is always less than other type)
            if (this.Type == IndexDataType.Null && other.Type == IndexDataType.Null) return 0;
            if (this.Type == IndexDataType.Null) return -1;
            if (other.Type == IndexDataType.Null) return 1;

            // if types are diferentes, convert
            if (this.Type != other.Type)
            {
                // if both values are number, convert them to Double to compare
                if (this.IsNumber && other.IsNumber)
                {
                    return Convert.ToDouble(this.Value).CompareTo(Convert.ToDouble(other.Value));
                }

                // if not, convert both to string
                return string.Compare(this.Value.ToString(), other.Value.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }

            // for both values with same datatype just compare
            switch(this.Type)
            {
                case IndexDataType.Int32: return ((Int32)this.Value).CompareTo((Int32)other.Value);
                case IndexDataType.Int64: return ((Int64)this.Value).CompareTo((Int64)other.Value);
                case IndexDataType.Double: return ((Double)this.Value).CompareTo((Double)other.Value);
                case IndexDataType.String: return string.Compare((String)this.Value, (String)other.Value, StringComparison.InvariantCultureIgnoreCase);
                case IndexDataType.Boolean: return ((Boolean)this.Value).CompareTo((Boolean)other.Value);
                case IndexDataType.DateTime: return ((DateTime)this.Value).CompareTo((DateTime)other.Value);
                case IndexDataType.Guid: return ((Guid)this.Value).CompareTo((Guid)other.Value);
            }

 	        throw new NotImplementedException();
        }

        public override string ToString()
        {
            return this.Value == null ? "(null)" : Value.ToString();
        }
    }
}
