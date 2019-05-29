using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB
{
    /// <summary>
    /// Calculate CRC-8 (1 byte)
    /// https://blog.csdn.net/plutus_sutulp/article/details/8473377
    /// </summary>
    internal static class Crc8
    {
        static byte[] _table = new byte[256];
        // x8 + x7 + x6 + x4 + x2 + 1
        const byte poly = 0xd5;

        public static byte ComputeChecksum(byte[] bytes, int offset, int count)
        {
            byte crc = 0;

            for(var i = 0; i < count; i++)
            {
                var b = bytes[offset + i];

                crc = _table[crc ^ b];
            }

            return crc;
        }

        static Crc8()
        {
            for (var i = 0; i < 256; ++i)
            {
                var temp = i;

                for (var j = 0; j < 8; ++j)
                {
                    if ((temp & 0x80) != 0)
                    {
                        temp = (temp << 1) ^ poly;
                    }
                    else
                    {
                        temp <<= 1;
                    }
                }

                _table[i] = (byte)temp;
            }
        }
    }
}
