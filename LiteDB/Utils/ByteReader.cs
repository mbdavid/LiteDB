using System;
using System.Text;

namespace LiteDB
{
    internal unsafe class ByteReader
    {
        private byte[] _buffer;
        private int _pos;

        public int Position { get { return _pos; } }

        public ByteReader(byte[] buffer)
        {
            _buffer = buffer;
            _pos = 0;
        }

        public void Skip(int length)
        {
            _pos += length;
        }

        #region Native data types

        public Byte ReadByte()
        {
            var value = _buffer[_pos];

            _pos++;

            return value;
        }

        public Boolean ReadBoolean()
        {
            var value = _buffer[_pos];

            _pos++;

            return value == 0 ? false : true;
        }

        public UInt16 ReadUInt16()
        {
            fixed (byte* numRef = &(_buffer[_pos]))
            {
                _pos += 2;
                return *(((UInt16*)numRef));
            }
        }

        public UInt32 ReadUInt32()
        {
            fixed (byte* numRef = &(_buffer[_pos]))
            {
                _pos += 4;
                return *(((UInt32*)numRef));
            }
        }

        public UInt64 ReadUInt64()
        {
            fixed (byte* numRef = &(_buffer[_pos]))
            {
                _pos += 8;
                return *(((UInt64*)numRef));
            }
        }

        public Int16 ReadInt16()
        {
            fixed (byte* numRef = &(_buffer[_pos]))
            {
                _pos += 2;
                return *(((Int16*)numRef));
            }
        }

        public Int32 ReadInt32()
        {
            fixed (byte* numRef = &(_buffer[_pos]))
            {
                _pos += 4;
                return *(((Int32*)numRef));
            }
        }

        public Int64 ReadInt64()
        {
            fixed (byte* numRef = &(_buffer[_pos]))
            {
                _pos += 8;
                return *(((Int64*)numRef));
            }
        }

        public Single ReadSingle()
        {
            fixed (byte* numRef = &(_buffer[_pos]))
            {
                _pos += 4;
                return *(((Single*)numRef));
            }
        }

        public Double ReadDouble()
        {
            fixed (byte* numRef = &(_buffer[_pos]))
            {
                _pos += 8;
                return *(((Double*)numRef));
            }
        }

        public Byte[] ReadBytes(int count)
        {
            var buffer = new byte[count];

            System.Buffer.BlockCopy(_buffer, _pos, buffer, 0, count);

            _pos += count;

            return buffer;
        }

        #endregion Native data types

        #region Extended types

        public string ReadString()
        {
            var length = this.ReadInt32();
            var bytes = this.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public string ReadString(int length)
        {
            var bytes = this.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public DateTime ReadDateTime()
        {
            return new DateTime(this.ReadInt64());
        }

        public Guid ReadGuid()
        {
            return new Guid(this.ReadBytes(16));
        }

        public ObjectId ReadObjectId()
        {
            return new ObjectId(this.ReadBytes(12));
        }

        public PageAddress ReadPageAddress()
        {
            return new PageAddress(this.ReadUInt32(), this.ReadUInt16());
        }

        public BsonValue ReadBsonValue(ushort length)
        {
            var type = (BsonType)this.ReadByte();

            switch (type)
            {
                case BsonType.Null: return BsonValue.Null;

                case BsonType.Int32: return this.ReadInt32();
                case BsonType.Int64: return this.ReadInt64();
                case BsonType.Double: return this.ReadDouble();

                case BsonType.String: return this.ReadString(length);

                case BsonType.Document: return new BsonReader().ReadDocument(this);
                case BsonType.Array: return new BsonReader().ReadArray(this);

                case BsonType.Binary: return this.ReadBytes(length);
                case BsonType.ObjectId: return this.ReadObjectId();
                case BsonType.Guid: return this.ReadGuid();

                case BsonType.Boolean: return this.ReadBoolean();
                case BsonType.DateTime: return this.ReadDateTime();

                case BsonType.MinValue: return BsonValue.MinValue;
                case BsonType.MaxValue: return BsonValue.MaxValue;
            }

            throw new NotImplementedException();
        }

        #endregion Extended types
    }
}