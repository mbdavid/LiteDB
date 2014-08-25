using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Represent a Bson Value used in BsonDocument
    /// </summary>
    public class BsonValue
    {
        private object _value = null;

        public BsonValue()
        {
            this.RawValue = null;
        }

        public BsonValue(object value)
        {
            this.RawValue = value;
        }

        public BsonType Type { get; private set; }

        public string[] Keys
        {
            get { return this.Type == BsonType.Object ? ((Dictionary<string, object>)_value).Keys.ToArray() : new string[0]; }
        }

        #region "this" operators

        public BsonValue this[string name]
        {
            get
            {
                if(this.Type != BsonType.Object) throw new LiteDBException("Bson value is not an object");

                var dict = (Dictionary<string, object>)_value;
                return new BsonValue(dict.Get(name));
            }
            set
            {
                if (this.Type != BsonType.Object) throw new LiteDBException("Bson value is not an object");

                var dict = (Dictionary<string, object>)_value;
                dict[name] = value.RawValue;
            }
        }

        public BsonValue this[int index]
        {
            get
            {
                if(this.Type != BsonType.Array) throw new LiteDBException("Bson value is not an array");

                var array = (List<object>)_value;
                return new BsonValue(array.ElementAt(index));
            }
            set
            {
                if(this.Type != BsonType.Array) throw new LiteDBException("Bson value is not an array");

                var array = (List<object>)_value;
                array[index] = value.RawValue;
            }
        }

        /// <summary>
        /// Same as doc[key] = value but with fluent api. Returns same object
        /// </summary>
        public BsonValue Append(string key, object value)
        {
            this[key] = new BsonValue(value);
            return this;
        }

        /// <summary>
        /// Read all first level of properties to get object values. doc.Append(new { Name = "John", Age = 55 });
        /// </summary>
        public BsonValue Append(object anonymousObject)
        {
            foreach (var prop in anonymousObject.GetType().GetProperties())
            {
                this[prop.Name] = new BsonValue(prop.GetValue(anonymousObject, null));
            }
            return this;
        }

        #endregion

        #region Array operations

        public void Add(object value)
        {
            if (this.Type != BsonType.Array) throw new LiteDBException("Bson value is not an array");

            var array = (List<object>)_value;
            array.Add(value);
        }

        public void Add(BsonValue value)
        {
            if (this.Type != BsonType.Array) throw new LiteDBException("Bson value is not an array");

            var array = (List<object>)_value;
            array.Add(value.RawValue);
        }

        public void Remove(int index)
        {
            if(this.Type != BsonType.Array) throw new LiteDBException("Bson value is not an array");

            var array = (List<object>)_value;
            array.RemoveAt(index);

        }

        public int Length
        {
            get
            {
                if (this.Type != BsonType.Array) return this.Keys.Length;

                var array = (List<object>)_value;
                return array.Count;
            }
        }

        #endregion

        #region Convert types

        public string AsString
        {
            get { return Type == BsonType.String ? (string)_value : null; }
        }

        public decimal AsDecimal
        {
            get { return Type == BsonType.Number ? Convert.ToDecimal(_value) : 0; }
        }

        public int AsInt
        {
            get { return Type == BsonType.Number ? Convert.ToInt32(_value) : 0; }
        }

        public bool AsBoolean
        {
            get { return Type == BsonType.Boolean ? (bool)_value : false; }
        }

        public DateTime AsDateTime
        {
            get { return Type == BsonType.DateTime ? (DateTime)_value : DateTime.MinValue; }
        }

        public Guid AsGuid
        {
            get { return Type == BsonType.Guid ? (Guid)_value : Guid.Empty; }
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

        public bool IsNumber
        {
            get { return this.Type == BsonType.Number; }
        }

        public bool IsBoolean
        {
            get { return this.Type == BsonType.Boolean; }
        }

        public bool IsObject
        {
            get { return this.Type == BsonType.Object; }
        }

        #endregion

        #region Raw Value

        public object RawValue
        {
            get { return _value; }
            set
            {
                _value = value;

                if (value == null)
                    this.Type = BsonType.Null;
                else if (value is bool)
                    this.Type = BsonType.Boolean;
                else if (value is string)
                    this.Type = BsonType.String;
                else if (value is byte || value is short || value is int || value is long || value is ushort || value is uint || value is ulong || value is decimal || value is double || value is float)
                    this.Type = BsonType.Number;
                else if (value is List<object>)
                    this.Type = BsonType.Array;
                else if (value is DateTime)
                    this.Type = BsonType.DateTime;
                else if (value is Guid)
                    this.Type = BsonType.Guid;
                else if (value is Dictionary<string, object>)
                    this.Type = BsonType.Object;
                else if (value is BsonValue)
                {
                    var v = (BsonValue)_value;
                    this.Type = v.Type;
                    _value = v.RawValue;
                }
                else
                    throw new LiteDBException("Bson value type unknow: " + value.GetType().FullName);
            }
        }

        #endregion

        #region Converters

        public static implicit operator string(BsonValue value)
        {
            return value.AsString;
        }

        public static implicit operator BsonValue(string value)
        {
            return new BsonValue(value);
        }

        public static implicit operator decimal(BsonValue value)
        {
            return value.AsDecimal;
        }

        public static implicit operator BsonValue(decimal value)
        {
            return new BsonValue(value);
        }

        public static implicit operator int(BsonValue value)
        {
            return value.AsInt;
        }

        public static implicit operator BsonValue(int value)
        {
            return new BsonValue(value);
        }

        public static implicit operator bool(BsonValue value)
        {
            return value.AsBoolean;
        }

        public static implicit operator BsonValue(bool value)
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
            return this.AsString;
        }

        #endregion
    }
}
