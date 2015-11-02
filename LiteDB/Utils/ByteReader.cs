using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LiteDB
{
    public unsafe class ByteReader
    {
        private byte[] _buffer;
        private int _pos;

        public int Position { get { return _pos; } }

        public ByteReader(byte[] buffer)
        {
            _buffer = buffer;
            _pos = 0;
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

        #endregion

    }
}