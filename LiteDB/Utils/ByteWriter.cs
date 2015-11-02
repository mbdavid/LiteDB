using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LiteDB
{
    public unsafe class ByteWriter
    {
        private byte[] _buffer;
        private int _index;

        public byte[] Buffer { get { return _buffer; } }

        public ByteWriter(int length)
        {
            _buffer = new byte[length];
            _index = 0;
        }

        public ByteWriter(byte[] buffer)
        {
            _buffer = buffer;
            _index = 0;
        }

        public void Write(byte value)
        {
            _buffer[_index] = value;

            _index++;
        }

        public void Write(bool value)
        {
            _buffer[_index] = value ? (byte)1 : (byte)1;

            _index++;
        }

        public void Write(ushort value)
        {
            byte* pi = (byte*)&value;

            _buffer[_index + 0] = pi[0];
            _buffer[_index + 1] = pi[1];

            _index += 2;
        }

        public void Write(uint value)
        {
            byte* pi = (byte*)&value;

            _buffer[_index + 0] = pi[0];
            _buffer[_index + 1] = pi[1];
            _buffer[_index + 2] = pi[2];
            _buffer[_index + 3] = pi[3];

            _index += 4;
        }

        public void Write(ulong value)
        {
            byte* pi = (byte*)&value;

            _buffer[_index + 0] = pi[0];
            _buffer[_index + 1] = pi[1];
            _buffer[_index + 2] = pi[2];
            _buffer[_index + 3] = pi[3];
            _buffer[_index + 4] = pi[4];
            _buffer[_index + 5] = pi[5];
            _buffer[_index + 6] = pi[6];
            _buffer[_index + 7] = pi[7];

            _index += 8;
        }

        public void Write(short value)
        {
            byte* pi = (byte*)&value;

            _buffer[_index + 0] = pi[0];
            _buffer[_index + 1] = pi[1];

            _index += 2;
        }

        public void Write(int value)
        {
            byte* pi = (byte*)&value;

            _buffer[_index + 0] = pi[0];
            _buffer[_index + 1] = pi[1];
            _buffer[_index + 2] = pi[2];
            _buffer[_index + 3] = pi[3];

            _index += 4;
        }

        public void Write(long value)
        {
            byte* pi = (byte*)&value;

            _buffer[_index + 0] = pi[0];
            _buffer[_index + 1] = pi[1];
            _buffer[_index + 2] = pi[2];
            _buffer[_index + 3] = pi[3];
            _buffer[_index + 4] = pi[4];
            _buffer[_index + 5] = pi[5];
            _buffer[_index + 6] = pi[6];
            _buffer[_index + 7] = pi[7];

            _index += 8;
        }

        public void Write(float value)
        {
            byte* pi = (byte*)&value;

            _buffer[_index + 0] = pi[0];
            _buffer[_index + 1] = pi[1];
            _buffer[_index + 2] = pi[2];
            _buffer[_index + 3] = pi[3];

            _index += 4;
        }

        public void Write(double value)
        {
            byte* pi = (byte*)&value;

            _buffer[_index + 0] = pi[0];
            _buffer[_index + 1] = pi[1];
            _buffer[_index + 2] = pi[2];
            _buffer[_index + 3] = pi[3];
            _buffer[_index + 4] = pi[4];
            _buffer[_index + 5] = pi[5];
            _buffer[_index + 6] = pi[6];
            _buffer[_index + 7] = pi[7];

            _index += 8;
        }

        public void Write(byte[] value)
        {
            System.Buffer.BlockCopy(value, 0, _buffer, _index, value.Length);

            _index += value.Length;
        }
    }
}