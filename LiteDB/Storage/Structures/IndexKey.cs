using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    //internal enum IndexDataType
    //{ 
    //    Null,
    //    Boolean,
    //    Int32,
    //    Int64,
    //    Double,
    //    String,
    //    DateTime,
    //    Guid
    //};

    /// <summary>
    /// Represent a index key value - can be a string, int, decimal, guid, ... It's persistable
    /// </summary>
    internal struct IndexKey : IComparable<IndexKey>
    {
        public const int MAX_LENGTH_SIZE = 255;

        public readonly BsonValue Value;

        public readonly int Length;

        public readonly bool IsNumber;

        public IndexKey(BsonValue value)
        {
            this.Value = value;
            this.IsNumber = value.IsNumber; // copy this property for better performance

            switch (value.Type)
            {
                // fixed length
                case BsonType.Null: this.Length = 0; break;
                case BsonType.Int32: this.Length = 4; break;
                case BsonType.Int64: this.Length = 8; break;
                case BsonType.Double: this.Length = 8; break;
                case BsonType.Boolean: this.Length = 1; break;
                case BsonType.DateTime: this.Length = 8; break;
                case BsonType.Guid: this.Length = 16; break;

                // variable length = +1 to store length
                case BsonType.Binary: this.Length = ((Byte[])value.RawValue).Length + 1; break;
                case BsonType.String:
                    // empty string convert to null
                    var str = ((string)Value.RawValue).Trim();
                    if (str.Length == 0)
                    {
                        this.Value = BsonValue.Null;
                        this.Length = 0;
                    }
                    else
                    {
                        this.Length = Encoding.UTF8.GetByteCount(str) + 1; break;
                    }
                    break;

                //TODO: implement for array/document: use ToString?
                case BsonType.Array: this.Length = 0; break;
                case BsonType.Document: this.Length = 0; break;
                default: this.Length = 0; break;
            }

            // increase "Type" byte in length
            this.Length++;

            // limit in 255 string bytes
            if (this.Length > MAX_LENGTH_SIZE)
            {
                throw LiteException.IndexKeyTooLong();
            }
        }

        public int CompareTo(IndexKey other)
        {
            return this.Value.CompareTo(other.Value);
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }
}
