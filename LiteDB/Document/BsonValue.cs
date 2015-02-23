using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Represent a Bson Value used in BsonDocument
    /// </summary>
    public class BsonValue : IComparable<BsonValue>, IEquatable<BsonValue>
    {
        public static BsonValue Null { get { return new BsonValue(); } }

        public BsonType Type { get; private set; }

        /// <summary>
        /// Gets .NET value object
        /// </summary>
        public virtual object RawValue { get; private set; }

        #region Constructor

        public BsonValue()
        {
            this.Type = BsonType.Null;
            this.RawValue = null;
        }

        public BsonValue(List<BsonValue> value)
        {
            this.Type = BsonType.Array;
            this.RawValue = value;
        }

        public BsonValue(Dictionary<string, BsonValue> value)
        {
            this.Type = BsonType.Document;
            this.RawValue = value;
        }

        public BsonValue(byte[] value)
        {
            this.Type = BsonType.Binary;
            this.RawValue = value;
        }

        public BsonValue(bool value)
        {
            this.Type = BsonType.Boolean;
            this.RawValue = value;
        }

        public BsonValue(string value)
        {
            this.Type = value == null ? BsonType.Null : BsonType.String;
            this.RawValue = value;
        }

        public BsonValue(int value)
        {
            this.Type = BsonType.Int32;
            this.RawValue = value;
        }

        public BsonValue(long value)
        {
            this.Type = BsonType.Int64;
            this.RawValue = value;
        }

        public BsonValue(double value)
        {
            this.Type = BsonType.Double;
            this.RawValue = value;
        }

        public BsonValue(DateTime value)
        {
            this.Type = BsonType.DateTime;
            this.RawValue = value;
        }

        public BsonValue(Guid value)
        {
            this.Type = BsonType.Guid;
            this.RawValue = value;
        }

        public BsonValue(BsonValue value)
        {
            this.Type = value.Type;
            this.RawValue = value.RawValue;
        }

        public BsonValue(object value)
        {
            this.RawValue = value;

            if (value == null) this.Type = BsonType.Null;
            else if (value is List<BsonValue>) this.Type = BsonType.Array;
            else if (value is Dictionary<string, BsonValue>) this.Type = BsonType.Document;
            else if (value is byte[]) this.Type = BsonType.Binary;
            else if (value is bool) this.Type = BsonType.Boolean;
            else if (value is string) this.Type = BsonType.String;
            else if (value is int) this.Type = BsonType.Int32;
            else if (value is long) this.Type = BsonType.Int64;
            else if (value is double) this.Type = BsonType.Double;
            else if (value is DateTime) this.Type = BsonType.DateTime;
            else if (value is Guid) this.Type = BsonType.Guid;
            else if (value is BsonValue)
            {
                var v = (BsonValue)value;
                this.Type = v.Type;
                this.RawValue = v.RawValue;
            }
            else throw new InvalidCastException("Value is not a valid BSON data type");
        }

        #endregion

        #region Convert types

        public BsonArray AsArray
        {
            get 
            {
                if (this.IsArray)
                {
                    return new BsonArray((List<BsonValue>)this.RawValue);
                }
                else
                {
                    return default(BsonArray);
                }
            }
        }

        public BsonDocument AsDocument
        {
            get
            {
                if (this.IsDocument)
                {
                    return new BsonDocument((Dictionary<string, BsonValue>)this.RawValue);
                }
                else
                {
                    return default(BsonDocument);
                }
            }
        }

        public Byte[] AsBinary
        {
            get { return this.Type == BsonType.Binary ? (Byte[])this.RawValue : default(Byte[]); }
        }

        public bool AsBoolean
        {
            get { return this.Type == BsonType.Boolean ? (Boolean)this.RawValue : default(Boolean); }
        }

        public string AsString
        {
            get { return this.Type != BsonType.Null ? this.RawValue.ToString() : default(String); }
        }

        public int AsInt32
        {
            get { return this.IsNumber ? Convert.ToInt32(this.RawValue) : default(Int32); }
        }

        public long AsInt64
        {
            get { return this.IsNumber ? Convert.ToInt64(this.RawValue) : default(Int64); }
        }

        public double AsDouble
        {
            get { return this.IsNumber ? Convert.ToDouble(this.RawValue) : default(Double); }
        }

        public DateTime AsDateTime
        {
            get { return this.Type == BsonType.DateTime ? (DateTime)this.RawValue : default(DateTime); }
        }

        public Guid AsGuid
        {
            get { return Type == BsonType.Guid ? (Guid)this.RawValue : default(Guid); }
        }

        #endregion

        #region IsTypes

        public bool IsNull
        {
            get { return this.Type == BsonType.Null; }
        }

        public bool IsArray
        {
            get { return this.Type == BsonType.Array; }
        }

        public bool IsDocument
        {
            get { return this.Type == BsonType.Document; }
        }

        public bool IsInt32
        {
            get { return this.Type == BsonType.Int32; }
        }

        public bool IsInt64
        {
            get { return this.Type == BsonType.Int64; }
        }

        public bool IsDouble
        {
            get { return this.Type == BsonType.Double; }
        }

        public bool IsNumber
        {
            get { return this.IsInt32 || this.IsInt64 || this.IsDouble; }
        }

        public bool IsBinary
        {
            get { return this.Type == BsonType.Binary; }
        }

        public bool IsBoolean
        {
            get { return this.Type == BsonType.Boolean; }
        }

        public bool IsString
        {
            get { return this.Type == BsonType.String; }
        }

        public bool IsGuid
        {
            get { return this.Type == BsonType.Guid; }
        }

        public bool IsDateTime
        {
            get { return this.Type == BsonType.DateTime; }
        }

        #endregion

        #region Methods for BsonDocument/BsonArray

        //public BsonValue this[string name]
        //{
        //    get
        //    {
        //        var dict = this.RawValue as Dictionary<string, BsonValue>;

        //        if (dict != null)
        //        {
        //            return dict.GetOrDefault(name, BsonValue.Null);
        //        }
        //        else
        //        {
        //            throw new LiteException("BsonValue is not a document");
        //        }
        //    }
        //    set
        //    {
        //        var dict = this.RawValue as Dictionary<string, BsonValue>;

        //        if (dict != null)
        //        {
        //            if (!BsonDocument.IsValidFieldName(name)) throw new ArgumentException(string.Format("Field name '{0}' is invalid pattern or reserved keyword", name));

        //            dict[name] = value ?? BsonValue.Null;
        //        }
        //        else
        //        {
        //            throw new LiteException("BsonValue is not a document");
        //        }
        //    }
        //}

        //public BsonValue this[int index]
        //{
        //    get
        //    {
        //        var list = this.RawValue as List<BsonValue>;

        //        if (list != null)
        //        {
        //            return list.ElementAt(index);
        //        }
        //        else
        //        {
        //            throw new LiteException("BsonValue is not an array");
        //        }
        //    }
        //    set
        //    {
        //        var list = this.RawValue as List<BsonValue>;

        //        if (list != null)
        //        {
        //            list[index] = value == null ? BsonValue.Null : value;
        //        }
        //        else
        //        {
        //            throw new LiteException("BsonValue is not an array");
        //        }
        //    }
        //}

        //public virtual void Add(BsonValue value)
        //{
        //    this.AsArray.Add(value);
        //}

        //public virtual BsonDocument Add(string key, BsonValue value)
        //{
        //    return this.AsDocument.Add(key, value);
        //}

        #endregion

        #region Implicit Ctor

        public static implicit operator Byte[](BsonValue value)
        {
            return (Byte[])value.RawValue;
        }

        public static implicit operator BsonValue(Byte[] value)
        {
            return new BsonValue { Type = BsonType.Binary, RawValue = value };
        }

        public static implicit operator Boolean(BsonValue value)
        {
            return (Boolean)value.RawValue;
        }

        public static implicit operator BsonValue(Boolean value)
        {
            return new BsonValue { Type = BsonType.Boolean, RawValue = value };
        }

        public static implicit operator String(BsonValue value)
        {
            return (String)value.RawValue;
        }

        public static implicit operator BsonValue(String value)
        {
            return new BsonValue { Type = BsonType.String, RawValue = value };
        }

        public static implicit operator Int32(BsonValue value)
        {
            return (Int32)value.RawValue;
        }

        public static implicit operator BsonValue(Int32 value)
        {
            return new BsonValue { Type = BsonType.Int32, RawValue = value };
        }

        public static implicit operator Int64(BsonValue value)
        {
            return (Int64)value.RawValue;
        }

        public static implicit operator BsonValue(Int64 value)
        {
            return new BsonValue { Type = BsonType.Int64, RawValue = value };
        }

        public static implicit operator Double(BsonValue value)
        {
            return (Double)value.RawValue;
        }

        public static implicit operator BsonValue(Double value)
        {
            return new BsonValue { Type = BsonType.Double, RawValue = value };
        }

        public static implicit operator DateTime(BsonValue value)
        {
            return (DateTime)value.RawValue;
        }

        public static implicit operator BsonValue(DateTime value)
        {
            return new BsonValue { Type = BsonType.DateTime, RawValue = value };
        }

        public static implicit operator Guid(BsonValue value)
        {
            return (Guid)value.RawValue;
        }

        public static implicit operator BsonValue(Guid value)
        {
            return new BsonValue { Type = BsonType.Guid, RawValue = value };
        }

        public static implicit operator List<BsonValue>(BsonValue value)
        {
            return (List<BsonValue>)value.RawValue;
        }

        public static implicit operator BsonValue(List<BsonValue> value)
        {
            return new BsonValue { Type = BsonType.Array, RawValue = value };
        }

        public static implicit operator Dictionary<string, BsonValue>(BsonValue value)
        {
            return (Dictionary<string, BsonValue>)value.RawValue;
        }

        public static implicit operator BsonValue(Dictionary<string, BsonValue> value)
        {
            return new BsonValue { Type = BsonType.Document, RawValue = value };
        }

        public override string ToString()
        {
            return this.IsNull ? "(null)" : this.RawValue.ToString();
        }

        #endregion

        #region IComparable<BsonValue>, IEquatable<BsonValue>

        public int CompareTo(BsonValue other)
        {
            // first, compare Null values (null is always less than other type)
            if (this.Type == BsonType.Null && other.Type == BsonType.Null) return 0;
            if (this.Type == BsonType.Null) return -1;
            if (other.Type == BsonType.Null) return 1;

            // if types are diferentes, convert
            if (this.Type != other.Type)
            {
                // if both values are number, convert them to Double to compare
                if (this.IsNumber && other.IsNumber)
                {
                    return Convert.ToDouble(this.RawValue).CompareTo(Convert.ToDouble(this.RawValue));
                }

                // array / object can't be compared
                if (this.IsDocument || this.IsArray || other.IsDocument || other.IsArray)
                {
                    throw new LiteException("BsonArray and BsonObject can't be compared");
                }
                else
                {
                    // if not, convert both to string
                    return string.Compare(this.RawValue.ToString(), other.RawValue.ToString());
                }
            }

            // for both values with same datatype just compare

            switch (this.Type)
            {
                case BsonType.Int32: return ((Int32)this.RawValue).CompareTo((Int32)other.RawValue);
                case BsonType.Int64: return ((Int64)this.RawValue).CompareTo((Int64)other.RawValue);
                case BsonType.Double: return ((Double)this.RawValue).CompareTo((Double)other.RawValue);
                case BsonType.String: return string.Compare((String)this.RawValue, (String)other.RawValue);
                case BsonType.Boolean: return ((Boolean)this.RawValue).CompareTo((Boolean)other.RawValue);
                case BsonType.DateTime: return ((DateTime)this.RawValue).CompareTo((DateTime)other.RawValue);
                case BsonType.Guid: return ((Guid)this.RawValue).CompareTo((Guid)other.RawValue);
            }

            throw new LiteException("BsonArray and BsonObject can't be compared");
        }

        public bool Equals(BsonValue other)
        {
            return this.CompareTo(other) == 0;
        }

        #endregion

        #region Operators == !=

        public static bool operator !=(BsonValue lhs, BsonValue rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(BsonValue lhs, BsonValue rhs)
        {
            if (object.ReferenceEquals(lhs, null)) return object.ReferenceEquals(rhs, null);
            if (object.ReferenceEquals(rhs, null)) return false; // don't check type because sometimes different types can be ==
            return lhs.CompareTo(rhs) == 0; // some subclasses override OperatorEqualsImplementation
        }

        public override bool Equals(object obj)
        {
            return this.Equals(new BsonValue(obj));
        }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = 37 * hash + this.Type.GetHashCode();
            hash = 37 * hash + this.RawValue.GetHashCode();
            return hash;
        }

        #endregion
    }
}
