using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Represent a Bson Value used in BsonDocument
    /// </summary>
    public class BsonValue : IComparable<BsonValue>, IEquatable<BsonValue>, IList<BsonValue>
    {
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Represent a Null bson type
        /// </summary>
        public static BsonValue Null => new BsonValue();

        /// <summary>
        /// Represent a MinValue bson type
        /// </summary>
        public static BsonValue MinValue => new BsonValue { Type = BsonType.MinValue, Value = "-oo" };

        /// <summary>
        /// Represent a MaxValue bson type
        /// </summary>
        public static BsonValue MaxValue => new BsonValue { Type = BsonType.MaxValue, Value = "+oo" };

        /// <summary>
        /// Create a new document used in DbRef => { $id: id, $ref: collection }
        /// </summary>
        public static BsonDocument DbRef(BsonValue id, string collection) => new BsonDocument { ["$id"] = id, ["$ref"] = collection };

        /// <summary>
        /// Indicate BsonType of this BsonValue
        /// </summary>
        public BsonType Type { get; private set; }

        /// <summary>
        /// Get internal .NET value object
        /// </summary>
        public object RawValue
        {
            get
            {
                switch (this.Type)
                {
                    case BsonType.Int32: return Int32Value;
                    case BsonType.Int64: return Int64Value;
                    case BsonType.Double: return DoubleValue;
                    case BsonType.Decimal: return DecimalValue;
                    case BsonType.String: return StringValue;
                    case BsonType.Document: return DocValue;
                    case BsonType.Array: return _arrayValue;
                    case BsonType.Binary: return BinaryValue;
                    case BsonType.ObjectId: return ObjectIdValue;
                    case BsonType.Guid: return GuidValue;
                    case BsonType.Boolean: return BoolValue;
                    case BsonType.DateTime: return DateTimeValue;
                    case BsonType.MinValue:
                    case BsonType.MaxValue:
                    case BsonType.Null:
                    default:
                        return StringValue;
                }
            }
        }

        /// <summary>
        /// Internal destroy method. Works only when used with BsonExpression
        /// </summary>
        internal Action Destroy = null;

        #region Constructor

        public BsonValue()
        {
            this.Type = BsonType.Null;
            this.Value = null;
        }

        public BsonValue(Int32 value)
        {
            this.Type = BsonType.Int32;
            this.Int32Value = value;
        }

        public BsonValue(Int64 value)
        {
            this.Type = BsonType.Int64;
            this.Int64Value = value;
        }

        public BsonValue(Double value)
        {
            this.Type = BsonType.Double;
            this.DoubleValue = value;
        }

        public BsonValue(Decimal value)
        {
            this.Type = BsonType.Decimal;
            this.DecimalValue = value;
        }

        public BsonValue(String value)
        {
            this.Type = value == null ? BsonType.Null : BsonType.String;
            this.StringValue = value;
        }

        public BsonValue(Dictionary<string, BsonValue> value)
        {
            this.Type = value == null ? BsonType.Null : BsonType.Document;
            this.DocValue = value;
        }

        public BsonValue(List<BsonValue> value)
        {
            this.Type = value == null ? BsonType.Null : BsonType.Array;
            this._arrayValue = value;
        }

        public BsonValue(BsonValue[] array) : this(array.ToList()) { }

        public BsonValue(IEnumerable<BsonValue> items) : this(items.ToList()) { }


        public BsonValue(Byte[] value)
        {
            this.Type = value == null ? BsonType.Null : BsonType.Binary;
            this.BinaryValue = value;
        }

        public BsonValue(ObjectId value)
        {
            this.Type = value == null ? BsonType.Null : BsonType.ObjectId;
            this.ObjectIdValue = value;
        }

        public BsonValue(Guid value)
        {
            this.Type = BsonType.Guid;
            this.GuidValue = value;
        }

        public BsonValue(Boolean value)
        {
            this.Type = BsonType.Boolean;
            this.BoolValue = value;
        }

        public BsonValue(DateTime value)
        {
            this.Type = BsonType.DateTime;
            this.DateTimeValue = value.Truncate();
        }

        public BsonValue(BsonValue value)
        {
            this.Type = value == null ? BsonType.Null : value.Type;
            switch (value.Type)
            {
                case BsonType.MinValue:
                    this.Value = value.Value;
                    break;
                case BsonType.Int32:
                    this.Int32Value = value.Int32Value;
                    break;
                case BsonType.Int64:
                    this.Int64Value = value.Int64Value;
                    break;
                case BsonType.Double:
                    this.DoubleValue = value.DoubleValue;
                    break;
                case BsonType.Decimal:
                    this.DecimalValue = value.DecimalValue;
                    break;
                case BsonType.String:
                    this.StringValue = value.StringValue;
                    break;
                case BsonType.Document:
                    this.DocValue = value.DocValue;
                    break;
                case BsonType.Array:
                    this._arrayValue = value._arrayValue;
                    break;
                case BsonType.Binary:
                    this.BinaryValue = value.BinaryValue;
                    break;
                case BsonType.ObjectId:
                    this.ObjectIdValue = value.ObjectIdValue;
                    break;
                case BsonType.Guid:
                    this.GuidValue = value.GuidValue;
                    break;
                case BsonType.Boolean:
                    this.BoolValue = value.BoolValue;
                    break;
                case BsonType.DateTime:
                    this.DateTimeValue = value.DateTimeValue;
                    break;
                case BsonType.MaxValue:
                    this.Value = value.Value;
                    break;
                case BsonType.Null:
                default:
                    break;
            }
        }

        public BsonValue(object value)
        {
            if (value == null) this.Type = BsonType.Null;
            else if (value is Int32 valInt32)
            {
                this.Type = BsonType.Int32;
                this.Int32Value = valInt32;
            }
            else if (value is Int64 valInt64)
            {
                this.Type = BsonType.Int64;
                this.Int64Value = valInt64;
            }
            else if (value is Double valDouble)
            {
                this.Type = BsonType.Double;
                this.DoubleValue = valDouble;
            }
            else if (value is Decimal valDecimal)
            {
                this.Type = BsonType.Decimal;
                this.DecimalValue = valDecimal;
            }
            else if (value is String valString)
            {
                this.Type = BsonType.String;
                this.StringValue = valString;
            }
            else if (value is Dictionary<string, BsonValue> valDoc)
            {
                this.Type = BsonType.Document;
                this.DocValue = valDoc;
            }
            else if (value is List<BsonValue> valArray)
            {
                this.Type = BsonType.Array;
                this._arrayValue = valArray;
            }
            else if (value is Byte[] valBinary)
            {
                this.Type = BsonType.Binary;
                this.BinaryValue = valBinary;
            }
            else if (value is ObjectId valObjectId)
            {
                this.Type = BsonType.ObjectId;
                this.ObjectIdValue = valObjectId;
            }
            else if (value is Guid valGuid)
            {
                this.Type = BsonType.Guid;
                this.GuidValue = valGuid;
            }
            else if (value is Boolean valBoolean)
            {
                this.Type = BsonType.Boolean;
                this.BoolValue = valBoolean;
            }
            else if (value is DateTime valDateTime)
            {
                this.Type = BsonType.DateTime;
                this.DateTimeValue = valDateTime.Truncate();
            }
            else if (value is BsonValue valBson)
            {
                this.Type = valBson.Type;
                switch (valBson.Type)
                {
                    case BsonType.MinValue:
                        this.Value = valBson.Value;
                        break;
                    case BsonType.Int32:
                        this.Int32Value = valBson.Int32Value;
                        break;
                    case BsonType.Int64:
                        this.Int64Value = valBson.Int64Value;
                        break;
                    case BsonType.Double:
                        this.DoubleValue = valBson.DoubleValue;
                        break;
                    case BsonType.Decimal:
                        this.DecimalValue = valBson.DecimalValue;
                        break;
                    case BsonType.String:
                        this.StringValue = valBson.StringValue;
                        break;
                    case BsonType.Document:
                        this.DocValue = valBson.DocValue;
                        break;
                    case BsonType.Array:
                        this._arrayValue = valBson._arrayValue;
                        break;
                    case BsonType.Binary:
                        this.BinaryValue = valBson.BinaryValue;
                        break;
                    case BsonType.ObjectId:
                        this.ObjectIdValue = valBson.ObjectIdValue;
                        break;
                    case BsonType.Guid:
                        this.GuidValue = valBson.GuidValue;
                        break;
                    case BsonType.Boolean:
                        this.BoolValue = valBson.BoolValue;
                        break;
                    case BsonType.DateTime:
                        this.DateTimeValue = valBson.DateTimeValue;
                        break;
                    case BsonType.MaxValue:
                        this.Value = valBson.Value;
                        break;
                    case BsonType.Null:
                    default:
                        break;
                }
            }
            else
            {
                // test for array or dictionary (document)
                var enumerable = value as System.Collections.IEnumerable;

                // test first for dictionary (because IDictionary implements IEnumerable)
                if (value is System.Collections.IDictionary dictionary)
                {
                    var dict = new Dictionary<string, BsonValue>(StringComparer.OrdinalIgnoreCase);

                    foreach (var key in dictionary.Keys)
                    {
                        dict.Add(key.ToString(), new BsonValue(dictionary[key]));
                    }

                    this.Type = BsonType.Document;
                    this.DocValue = dict;
                }
                else if (enumerable != null)
                {
                    var list = new List<BsonValue>();

                    foreach (var x in enumerable)
                    {
                        list.Add(new BsonValue(x));
                    }

                    this.Type = BsonType.Array;
                    this._arrayValue = list;
                }
                else
                {
                    throw new InvalidCastException("Value is not a valid BSON data type - Use Mapper.ToDocument for more complex types converts");
                }
            }
        }

        #endregion

        #region Index "this" property

        /// <summary>
        /// Get/Set a field for document. Fields are case sensitive - Works only when value are document
        /// </summary>
        public virtual BsonValue this[string name]
        {
            get => throw new InvalidOperationException("Cannot access non-document type value on " + this.RawValue);
            set => throw new InvalidOperationException("Cannot access non-document type value on " + this.RawValue);
        }

        #endregion

        #region Array implementation

        /// <summary>
        /// Get/Set value in array position. Works only when value are array
        /// </summary>
        public virtual BsonValue this[int index]
        {
            get
            {
                if (!IsArray)
                    throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

                return _arrayValue[index];
            }
            set
            {
                if (!IsArray)
                    throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

                _arrayValue[index] = value;
            }
        }

        public int Count
        {
            get
            {
                if (!IsArray)
                    throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

                return _arrayValue.Count;
            }
        }

        public bool IsReadOnly => false;

        public int IndexOf(BsonValue item)
        {
            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            return _arrayValue.IndexOf(item);
        }

        public void Insert(int index, BsonValue item)
        {
            if (Type == BsonType.Null)
            {
                Type = BsonType.Array;
                _arrayValue = new List<BsonValue>();
            }

            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            _arrayValue.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            _arrayValue.RemoveAt(index);
        }

        public void Add(BsonValue item)
        {
            if (Type == BsonType.Null)
            {
                Type = BsonType.Array;
                _arrayValue = new List<BsonValue>();
            }

            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            _arrayValue.Add(item);
        }

        public void AddRange<T>(IEnumerable<T> array) where T : BsonValue
        {
            if (Type == BsonType.Null)
            {
                Type = BsonType.Array;
                _arrayValue = new List<BsonValue>();
            }

            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            if (array == null)
                throw new ArgumentNullException(nameof(array));

            foreach (var item in array)
                Add(item ?? BsonValue.Null);
        }

        public void Clear()
        {
            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            _arrayValue.Clear();
        }

        public bool Contains(BsonValue item)
        {
            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            return _arrayValue.Contains(item);
        }

        public void CopyTo(BsonValue[] array, int arrayIndex)
        {
            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            _arrayValue.CopyTo(array, arrayIndex);
        }

        public bool Remove(BsonValue item)
        {
            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            return _arrayValue.Remove(item);
        }

        public IEnumerator<BsonValue> GetEnumerator()
        {
            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            return _arrayValue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (!IsArray)
                throw new InvalidOperationException("Cannot access non-array type value on " + this.RawValue);

            foreach (var value in _arrayValue)
                yield return new BsonValue(value);
        }


        #endregion

        #region Convert types

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BsonDocument AsDocument
        {
            get
            {
                if (this.IsDocument)
                    return new BsonDocument(DocValue)
                    {
                        Length = this.Length,
                        Destroy = this.Destroy
                    };
                else
                    return default(BsonDocument);
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Byte[] AsBinary => this.Type == BsonType.Binary ? BinaryValue : default(Byte[]);


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool AsBoolean => this.Type == BsonType.Boolean ? BoolValue : default(Boolean);


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string AsString
        {
            get
            {
                switch (this.Type)
                {
                    case BsonType.MinValue:
                        return "-oo";
                    case BsonType.Int32:
                        return Int32Value.ToString();
                    case BsonType.Int64:
                        return Int64Value.ToString();
                    case BsonType.Double:
                        return DoubleValue.ToString();
                    case BsonType.Decimal:
                        return DecimalValue.ToString();
                    case BsonType.String:
                        return StringValue;
                    case BsonType.Document:
                        return JsonSerializer.Serialize(this);
                    case BsonType.Array:
                        return JsonSerializer.Serialize(this);
                    case BsonType.Binary:
                        return BinaryValue.ToString();
                    case BsonType.ObjectId:
                        return ObjectIdValue.ToString();
                    case BsonType.Guid:
                        return GuidValue.ToString();
                    case BsonType.Boolean:
                        return BoolValue.ToString();
                    case BsonType.DateTime:
                        return DateTimeValue.ToString();
                    case BsonType.MaxValue:
                        return "+oo";
                    case BsonType.Null:
                    default:
                        return default(String);
                }
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int AsInt32
        {
            get
            {
                if (this.IsInt32)
                    return Int32Value;
                else if (this.IsInt64)
                    return Convert.ToInt32(Int64Value);
                else if (this.IsDouble)
                    return Convert.ToInt32(DoubleValue);
                else if (IsDecimal)
                    return Convert.ToInt32(DecimalValue);

                return default(Int32);
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public long AsInt64
        {
            get
            {
                if (this.IsInt32)
                    return Convert.ToInt64(Int32Value);
                else if (this.IsInt64)
                    return Int64Value;
                else if (this.IsDouble)
                    return Convert.ToInt64(DoubleValue);
                else if (IsDecimal)
                    return Convert.ToInt64(DecimalValue);

                return default(Int64);
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double AsDouble
        {
            get
            {
                if (this.IsInt32)
                    return Convert.ToDouble(Int32Value);
                else if (this.IsInt64)
                    return Convert.ToDouble(Int64Value);
                else if (this.IsDouble)
                    return DoubleValue;
                else if (IsDecimal)
                    return Convert.ToDouble(DecimalValue);

                return default(Double);
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public decimal AsDecimal
        {
            get
            {
                if (this.IsInt32)
                    return Convert.ToDecimal(Int32Value);
                else if (this.IsInt64)
                    return Convert.ToDecimal(Int64Value);
                else if (this.IsDouble)
                    return Convert.ToDecimal(DoubleValue);
                else if (IsDecimal)
                    return DecimalValue;

                return default(Decimal);
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public DateTime AsDateTime => this.Type == BsonType.DateTime ? DateTimeValue : default(DateTime);


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ObjectId AsObjectId => this.Type == BsonType.ObjectId ? ObjectIdValue : default(ObjectId);


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Guid AsGuid => this.Type == BsonType.Guid ? GuidValue : default(Guid);

        #endregion

        #region IsTypes

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsNull => this.Type == BsonType.Null;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsArray => this.Type == BsonType.Array;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsDocument => this.Type == BsonType.Document;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsInt32 => this.Type == BsonType.Int32;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsInt64 => this.Type == BsonType.Int64;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsDouble => this.Type == BsonType.Double;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsDecimal => this.Type == BsonType.Decimal;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsNumber => this.IsInt32 || this.IsInt64 || this.IsDouble || this.IsDecimal;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsBinary => this.Type == BsonType.Binary;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsBoolean => this.Type == BsonType.Boolean;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsString => this.Type == BsonType.String;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsObjectId => this.Type == BsonType.ObjectId;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsGuid => this.Type == BsonType.Guid;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsDateTime => this.Type == BsonType.DateTime;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsMinValue => this.Type == BsonType.MinValue;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsMaxValue => this.Type == BsonType.MaxValue;

        #endregion

        #region Values

        public String Value { get; private set; }
        public Int32 Int32Value { get; private set; }
        public Int64 Int64Value { get; private set; }
        public Double DoubleValue { get; private set; }
        public Decimal DecimalValue { get; private set; }
        public UInt64 Uint64Value { get; private set; }
        public String StringValue { get; private set; }
        public Dictionary<string, BsonValue> DocValue { get; private set; }
        private List<BsonValue> _arrayValue { get; set; }
        public Byte[] BinaryValue { get; private set; }
        public ObjectId ObjectIdValue { get; private set; }
        public Guid GuidValue { get; private set; }
        public bool BoolValue { get; private set; }
        public DateTime DateTimeValue { get; private set; }

        #endregion

        #region Implicit Ctor

        // Int32
        public static implicit operator Int32(BsonValue value) => value.Int32Value;

        // Int32
        public static implicit operator BsonValue(Int32 value) => new BsonValue(value);

        // Int64
        public static implicit operator Int64(BsonValue value) => value.Int64Value;

        // Int64
        public static implicit operator BsonValue(Int64 value) => new BsonValue(value);

        // Double
        public static implicit operator Double(BsonValue value) => value.DoubleValue;

        // Double
        public static implicit operator BsonValue(Double value) => new BsonValue(value);

        // Decimal
        public static implicit operator Decimal(BsonValue value) => value.DecimalValue;

        // Decimal
        public static implicit operator BsonValue(Decimal value) => new BsonValue(value);

        // UInt64 (to avoid ambigous between Double-Decimal)
        public static implicit operator UInt64(BsonValue value) => value.Uint64Value;

        // Decimal
        public static implicit operator BsonValue(UInt64 value) => new BsonValue((double)value);

        // String
        public static implicit operator String(BsonValue value) => value.StringValue;

        // String
        public static implicit operator BsonValue(String value) => new BsonValue(value);

        // Document
        public static implicit operator Dictionary<string, BsonValue>(BsonValue value) => value.DocValue;

        // Document
        public static implicit operator BsonValue(Dictionary<string, BsonValue> value) => new BsonValue(value);

        // Array
        public static implicit operator List<BsonValue>(BsonValue value) => value._arrayValue;

        // Array
        public static implicit operator BsonValue(List<BsonValue> value) => new BsonValue(value);

        // Binary
        public static implicit operator Byte[] (BsonValue value) => value.BinaryValue;

        // Binary
        public static implicit operator BsonValue(Byte[] value) => new BsonValue(value);

        // ObjectId
        public static implicit operator ObjectId(BsonValue value) => value.ObjectIdValue;

        // ObjectId
        public static implicit operator BsonValue(ObjectId value) => new BsonValue(value);

        // Guid
        public static implicit operator Guid(BsonValue value) => value.GuidValue;

        // Guid
        public static implicit operator BsonValue(Guid value) => new BsonValue(value);

        // Boolean
        public static implicit operator Boolean(BsonValue value) => value.BoolValue;

        // Boolean
        public static implicit operator BsonValue(Boolean value) => new BsonValue(value);

        // DateTime
        public static implicit operator DateTime(BsonValue value) => value.DateTimeValue;

        // DateTime
        public static implicit operator BsonValue(DateTime value) => new BsonValue(value);

        // +
        public static BsonValue operator +(BsonValue left, BsonValue right)
        {
            if (!left.IsNumber || !right.IsNumber) return BsonValue.Null;

            if (left.IsInt32 && right.IsInt32) return left.AsInt32 + right.AsInt32;
            if (left.IsInt64 && right.IsInt64) return left.AsInt64 + right.AsInt64;
            if (left.IsDouble && right.IsDouble) return left.AsDouble + right.AsDouble;
            if (left.IsDecimal && right.IsDecimal) return left.AsDecimal + right.AsDecimal;

            var result = left.AsDecimal + right.AsDecimal;
            var type = (BsonType)Math.Max((int)left.Type, (int)right.Type);

            return
                type == BsonType.Int64 ? new BsonValue((Int64)result) :
                type == BsonType.Double ? new BsonValue((Double)result) :
                new BsonValue(result);
        }

        // -
        public static BsonValue operator -(BsonValue left, BsonValue right)
        {
            if (!left.IsNumber || !right.IsNumber) return BsonValue.Null;

            if (left.IsInt32 && right.IsInt32) return left.AsInt32 - right.AsInt32;
            if (left.IsInt64 && right.IsInt64) return left.AsInt64 - right.AsInt64;
            if (left.IsDouble && right.IsDouble) return left.AsDouble - right.AsDouble;
            if (left.IsDecimal && right.IsDecimal) return left.AsDecimal - right.AsDecimal;

            var result = left.AsDecimal - right.AsDecimal;
            var type = (BsonType)Math.Max((int)left.Type, (int)right.Type);

            return
                type == BsonType.Int64 ? new BsonValue((Int64)result) :
                type == BsonType.Double ? new BsonValue((Double)result) :
                new BsonValue(result);
        }

        // *
        public static BsonValue operator *(BsonValue left, BsonValue right)
        {
            if (!left.IsNumber || !right.IsNumber) return BsonValue.Null;

            if (left.IsInt32 && right.IsInt32) return left.AsInt32 * right.AsInt32;
            if (left.IsInt64 && right.IsInt64) return left.AsInt64 * right.AsInt64;
            if (left.IsDouble && right.IsDouble) return left.AsDouble * right.AsDouble;
            if (left.IsDecimal && right.IsDecimal) return left.AsDecimal * right.AsDecimal;

            var result = left.AsDecimal * right.AsDecimal;
            var type = (BsonType)Math.Max((int)left.Type, (int)right.Type);

            return
                type == BsonType.Int64 ? new BsonValue((Int64)result) :
                type == BsonType.Double ? new BsonValue((Double)result) :
                new BsonValue(result);
        }

        // /
        public static BsonValue operator /(BsonValue left, BsonValue right)
        {
            if (!left.IsNumber || !right.IsNumber) return BsonValue.Null;
            if (left.IsDecimal || right.IsDecimal) return left.AsDecimal / right.AsDecimal;

            return left.AsDouble / right.AsDouble;
        }

        public override string ToString() => AsString;

        #endregion

        #region IComparable<BsonValue>, IEquatable<BsonValue>

        public virtual int CompareTo(BsonValue other)
        {
            // first, test if types are different
            if (this.Type != other.Type)
            {
                // if both values are number, convert them to Decimal (128 bits) to compare
                // it's the slowest way, but more secure
                if (this.IsNumber && other.IsNumber)
                    return AsDecimal.CompareTo(other.AsDecimal);
                // if not, order by sort type order
                else
                    return this.Type.CompareTo(other.Type);
            }

            // for both values with same data type just compare
            switch (this.Type)
            {
                case BsonType.Null:
                case BsonType.MinValue:
                case BsonType.MaxValue:
                    return 0;

                case BsonType.Int32: return Int32Value.CompareTo(other.Int32Value);
                case BsonType.Int64: return Int64Value.CompareTo(other.Int64Value);
                case BsonType.Double: return DoubleValue.CompareTo(other.DoubleValue);
                case BsonType.Decimal: return DecimalValue.CompareTo(other.DecimalValue);

                case BsonType.String: return string.Compare(StringValue, other.StringValue);

                case BsonType.Document: return this.AsDocument.CompareTo(other);
                case BsonType.Array:
                    {
                        var result = 0;
                        var i = 0;
                        var stop = Math.Min(_arrayValue.Count, other._arrayValue.Count);

                        // compare each element
                        for (; 0 == result && i < stop; i++)
                            result = _arrayValue[i].CompareTo(other._arrayValue[i]);

                        if (result != 0)
                            return result;

                        if (i == _arrayValue.Count)
                            return i == other._arrayValue.Count ? 0 : -1;

                        return 1;
                    }

                case BsonType.Binary: return BinaryValue.BinaryCompareTo(other.BinaryValue);
                case BsonType.ObjectId: return ObjectIdValue.CompareTo(other.ObjectIdValue);
                case BsonType.Guid: return GuidValue.CompareTo(other.GuidValue);

                case BsonType.Boolean: return BoolValue.CompareTo(other.BoolValue);
                case BsonType.DateTime:
                    var d0 = DateTimeValue;
                    var d1 = other.DateTimeValue;
                    if (d0.Kind != DateTimeKind.Utc) d0 = d0.ToUniversalTime();
                    if (d1.Kind != DateTimeKind.Utc) d1 = d1.ToUniversalTime();
                    return d0.CompareTo(d1);

                default: throw new NotImplementedException();
            }
        }


        public bool Equals(BsonValue other) => this.CompareTo(other) == 0;

        #endregion

        #region Operators

        public static bool operator ==(BsonValue lhs, BsonValue rhs)
        {
            if (object.ReferenceEquals(lhs, null)) return object.ReferenceEquals(rhs, null);
            if (object.ReferenceEquals(rhs, null)) return false; // don't check type because sometimes different types can be ==

            return lhs.Equals(rhs);
        }


        public static bool operator !=(BsonValue lhs, BsonValue rhs) => !(lhs == rhs);


        public static bool operator >=(BsonValue lhs, BsonValue rhs) => lhs.CompareTo(rhs) >= 0;


        public static bool operator >(BsonValue lhs, BsonValue rhs) => lhs.CompareTo(rhs) > 0;


        public static bool operator <(BsonValue lhs, BsonValue rhs) => lhs.CompareTo(rhs) < 0;


        public static bool operator <=(BsonValue lhs, BsonValue rhs) => lhs.CompareTo(rhs) <= 0;


        public override bool Equals(object obj) => this.Equals(new BsonValue(obj));


        public override int GetHashCode()
        {
            var hash = 17 * 37;
            hash += this.Type.GetHashCode();
            hash *= 37;

            switch (this.Type)
            {
                case BsonType.MinValue:
                    hash += this.Value.GetHashCode();
                    break;
                case BsonType.Int32:
                    hash += this.Int32Value.GetHashCode();
                    break;
                case BsonType.Int64:
                    hash += this.Int64Value.GetHashCode();
                    break;
                case BsonType.Double:
                    hash += this.DoubleValue.GetHashCode();
                    break;
                case BsonType.Decimal:
                    hash += this.DecimalValue.GetHashCode();
                    break;
                case BsonType.String:
                    hash += this.StringValue.GetHashCode();
                    break;
                case BsonType.Document:
                    hash += this.DocValue.GetHashCode();
                    break;
                case BsonType.Array:
                    hash += this._arrayValue.GetHashCode();
                    break;
                case BsonType.Binary:
                    hash += this.BinaryValue.GetHashCode();
                    break;
                case BsonType.ObjectId:
                    hash += this.ObjectIdValue.GetHashCode();
                    break;
                case BsonType.Guid:
                    hash += this.GuidValue.GetHashCode();
                    break;
                case BsonType.Boolean:
                    hash += this.BoolValue.GetHashCode();
                    break;
                case BsonType.DateTime:
                    hash += this.DateTimeValue.GetHashCode();
                    break;
                case BsonType.MaxValue:
                    hash += this.Value.GetHashCode();
                    break;
                case BsonType.Null:
                default:
                    break;
            }

            return hash;
        }

        #endregion

        #region GetBytesCount

        internal int? Length = null;

        /// <summary>
        /// Returns how many bytes this BsonValue will use to persist in index writes
        /// </summary>
        public int GetBytesCount(bool recalc)
        {
            if (recalc == false && this.Length.HasValue) return this.Length.Value;

            switch (this.Type)
            {
                case BsonType.Null:
                case BsonType.MinValue:
                case BsonType.MaxValue:
                    this.Length = 0; break;

                case BsonType.Int32: this.Length = 4; break;
                case BsonType.Int64: this.Length = 8; break;
                case BsonType.Double: this.Length = 8; break;
                case BsonType.Decimal: this.Length = 16; break;

                case BsonType.String: this.Length = Encoding.UTF8.GetByteCount(StringValue); break;

                case BsonType.Binary: this.Length = BinaryValue.Length; break;
                case BsonType.ObjectId: this.Length = 12; break;
                case BsonType.Guid: this.Length = 16; break;

                case BsonType.Boolean: this.Length = 1; break;
                case BsonType.DateTime: this.Length = 8; break;

                // for Array/Document calculate from elements
                case BsonType.Array:
                    this.Length = 5; // header + footer
                    for (var i = 0; i < _arrayValue.Count; i++)
                    {
                        this.Length += this.GetBytesCountElement(i.ToString(), _arrayValue[i] ?? BsonValue.Null, recalc);
                    }
                    break;

                case BsonType.Document:
                    this.Length = 5; // header + footer
                    foreach (var key in DocValue.Keys)
                    {
                        this.Length += this.GetBytesCountElement(key, DocValue[key] ?? BsonValue.Null, recalc);
                    }
                    break;
            }

            return this.Length.Value;
        }

        private int GetBytesCountElement(string key, BsonValue value, bool recalc)
        {
            return
                1 + // element type
                Encoding.UTF8.GetByteCount(key) + // CString
                1 + // CString 0x00
                value.GetBytesCount(recalc) +
                (value.Type == BsonType.String || value.Type == BsonType.Binary || value.Type == BsonType.Guid ? 5 : 0); // bytes.Length + 0x??
        }

        #endregion
    }
}
