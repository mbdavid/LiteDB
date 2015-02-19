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
    public class BsonValue
    {
        public static Regex PropertyPattern = new Regex(@"^\w[\w_-]*$");

        public BsonType Type { get; private set; }
        public virtual object RawValue { get; private set; }

        public static readonly BsonValue Null = new BsonValue();

        #region Constructor

        public BsonValue()
        {
            this.Type = BsonType.Null;
        }

        public BsonValue(List<BsonValue> value)
        {
            this.Type = BsonType.Array;
            this.RawValue = value;
        }

        public BsonValue(Dictionary<string, BsonValue> value)
        {
            this.Type = BsonType.Object;
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
            else if (value is List<object>) this.Type = BsonType.Array;
            else if (value is Dictionary<string, object>) this.Type = BsonType.Object;
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
            else throw new InvalidCastException("Value is not a valid data type");
        }

        #endregion

        #region "this" operators for BsonObject/BsonArray

        public BsonValue this[string name]
        {
            get
            {
                return this.AsObject.RawValue.GetOrDefault(name, BsonValue.Null);
            }
            set
            {
                if (!PropertyPattern.IsMatch(name)) throw new ArgumentException(string.Format("Property name '{0}' is invalid pattern", name));

                this.AsObject.RawValue[name] = value == null ? BsonValue.Null : value;
            }
        }

        public BsonValue this[int index]
        {
            get
            {
                return this.AsArray.RawValue.ElementAt(index);
            }
            set
            {
                this.AsArray.RawValue[index] = value == null ? BsonValue.Null : value;
            }
        }

        #endregion

        #region Convert types

        public BsonArray AsArray
        {
            get 
            {
                if (this.IsArray) return new BsonArray((List<BsonValue>)this.RawValue);
                throw new LiteException("Value is not an array");
            }
        }

        public BsonObject AsObject
        {
            get
            {
                if (this.IsObject) return new BsonObject((Dictionary<string, BsonValue>)this.RawValue);
                throw new LiteException("Value is not an object");
            }
        }

        public BsonDocument AsDocument
        {
            get
            {
                if (!this.IsObject) throw new LiteException("Value is not an document");
                return new BsonDocument(this.AsObject);
            }
        }

        public Byte[] AsBinary
        {
            get { return this.Type == BsonType.Binary ? (Byte[])this.RawValue : default(Byte[]); }
            set { this.Type = BsonType.Binary; this.RawValue = value; }
        }

        public bool AsBoolean
        {
            get { return this.Type == BsonType.Boolean ? (Boolean)this.RawValue : default(Boolean); }
            set { this.Type = BsonType.Boolean; this.RawValue = value; }
        }

        public string AsString
        {
            get { return this.Type != BsonType.Null ? (String)this.RawValue : default(String); }
            set { this.Type = value == null ? BsonType.Null : BsonType.String; this.RawValue = value; }
        }

        public int AsInt32
        {
            get { return this.IsNumber ? Convert.ToInt32(this.RawValue) : default(Int32); }
            set { this.Type = BsonType.Int32; this.RawValue = value; }
        }

        public long AsInt64
        {
            get { return this.IsNumber ? Convert.ToInt64(this.RawValue) : default(Int64); }
            set { this.Type = BsonType.Int64; this.RawValue = value; }
        }

        public double AsDouble
        {
            get { return this.IsNumber ? Convert.ToDouble(this.RawValue) : default(Double); }
            set { this.Type = BsonType.Double; this.RawValue = value; }
        }

        public DateTime AsDateTime
        {
            get { return this.Type == BsonType.DateTime ? (DateTime)this.RawValue : default(DateTime); }
            set { this.Type = BsonType.DateTime; this.RawValue = value; }
        }

        public Guid AsGuid
        {
            get { return Type == BsonType.Guid ? (Guid)this.RawValue : default(Guid); }
            set { this.Type = BsonType.Guid; this.RawValue = value; }
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

        public bool IsObject
        {
            get { return this.Type == BsonType.Object; }
        }

        public bool IsDocument
        {
            get { return this.Type == BsonType.Object && !this["_id"].IsNull; }
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

        #region Operators

        public static implicit operator Byte[](BsonValue value)
        {
            return value.AsBinary;
        }

        public static implicit operator BsonValue(Byte[] value)
        {
            return new BsonValue(value);
        }

        public static implicit operator Boolean(BsonValue value)
        {
            return value.AsBoolean;
        }

        public static implicit operator BsonValue(Boolean value)
        {
            return new BsonValue(value);
        }

        public static implicit operator String(BsonValue value)
        {
            return value.AsString;
        }

        public static implicit operator BsonValue(String value)
        {
            return new BsonValue(value);
        }

        public static implicit operator Int32(BsonValue value)
        {
            return value.AsInt32;
        }

        public static implicit operator BsonValue(Int32 value)
        {
            return new BsonValue(value);
        }

        public static implicit operator Int64(BsonValue value)
        {
            return value.AsInt64;
        }

        public static implicit operator BsonValue(Int64 value)
        {
            return new BsonValue(value);
        }

        public static implicit operator Double(BsonValue value)
        {
            return value.AsDouble;
        }

        public static implicit operator BsonValue(Double value)
        {
            return new BsonValue(value);
        }

        public static implicit operator DateTime(BsonValue value)
        {
            return value.AsDateTime;
        }

        public static implicit operator BsonValue(DateTime value)
        {
            return new BsonValue(value);
        }

        public static implicit operator Guid(BsonValue value)
        {
            return value.AsGuid;
        }

        public static implicit operator BsonValue(Guid value)
        {
            return new BsonValue(value);
        }

        public override string ToString()
        {
            return this.IsNull ? "(null)" : this.RawValue.ToString();
        }

        #endregion
    }
}
