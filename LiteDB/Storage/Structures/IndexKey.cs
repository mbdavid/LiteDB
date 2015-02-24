using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Represent a index key value - it´s a BsonValue 
    /// </summary>
    internal struct IndexKey
    {
        public const int MAX_LENGTH_SIZE = 255;

        public readonly BsonValue Value;

        public readonly int Length;

        public IndexKey(BsonValue value)
        {
            this.Value = value;

            // init Lenth with 1 to "Type" byte
            this.Length = 1;

            switch (value.Type)
            {
                // fixed length
                case BsonType.Null: this.Length += 0; break;

                case BsonType.Int32: this.Length += 4; break;
                case BsonType.Int64: this.Length += 8; break;
                case BsonType.Double: this.Length += 8; break;

                case BsonType.Boolean: this.Length += 1; break;
                case BsonType.DateTime: this.Length += 8; break;
                case BsonType.Guid: this.Length += 16; break;

                // variable length = +1 to store length
                case BsonType.Binary: this.Length += ((Byte[])value.RawValue).Length + 1; break;
                case BsonType.String:
                    // empty string convert to null
                    var str = ((string)Value.RawValue).Trim();
                    if (str.Length == 0)
                    {
                        this.Value = BsonValue.Null;
                        this.Length += 0;
                    }
                    else
                    {
                        this.Length += Encoding.UTF8.GetByteCount(str) + 1; break;
                    }
                    break;

                //TODO: implement for array/document: use Json/Bson?
                case BsonType.Array: this.Length = 0; break;
                case BsonType.Document: this.Length = 0; break;
            }

            // limit in 255 string bytes
            if (this.Length > MAX_LENGTH_SIZE)
            {
                throw LiteException.IndexKeyTooLong();
            }
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }
}
