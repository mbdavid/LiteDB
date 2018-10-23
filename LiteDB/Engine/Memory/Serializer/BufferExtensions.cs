using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LiteDB.Engine
{
    internal static class BufferExtensions
    {
        #region ArraySegmentExtensions

        /// <summary>
        /// Same as Get index [int]
        /// </summary>
        public static byte Get(this ArraySegment<byte> segment, int index)
        {
            return segment.Array[segment.Offset + index];
        }

        /// <summary>
        /// Same as Set index [int]
        /// </summary>
        public static void Set(this ArraySegment<byte> segment, int index, byte value)
        {
            segment.Array[segment.Offset + index] = value;
        }

        /// <summary>
        /// Create new ArraySegment based on new offset/count over current segment
        /// </summary>
        public static ArraySegment<byte> Slice(this ArraySegment<byte> segment, int offset, int count)
        {
            return new ArraySegment<byte>(segment.Array, segment.Offset + offset, count);
        }

        #endregion

        #region ToBytes BitConverter helper

        /// <summary>
        /// Copy Int16 bytes direct into buffer
        /// </summary>
        public unsafe static void ToBytes(this Int16 value, byte[] array, int startIndex)
        {
            fixed (byte* ptr = &array[startIndex])
            {
                *(Int16*)ptr = value;
            }
        }

        /// <summary>
        /// Copy Int32 bytes direct into buffer
        /// </summary>
        public unsafe static void ToBytes(this Int32 value, byte[] array, int startIndex)
        {
            fixed (byte* ptr = &array[startIndex])
            {
                *(Int32*)ptr = value;
            }
        }

        /// <summary>
        /// Copy Int64 bytes direct into buffer
        /// </summary>
        public unsafe static void ToBytes(this Int64 value, byte[] array, int startIndex)
        {
            fixed (byte* ptr = &array[startIndex])
            {
                *(Int64*)ptr = value;
            }
        }

        /// <summary>
        /// Copy UInt16 bytes direct into buffer
        /// </summary>
        public unsafe static void ToBytes(this UInt16 value, byte[] array, int startIndex)
        {
            fixed (byte* ptr = &array[startIndex])
            {
                *(UInt16*)ptr = value;
            }
        }

        /// <summary>
        /// Copy UInt32 bytes direct into buffer
        /// </summary>
        public unsafe static void ToBytes(this UInt32 value, byte[] array, int startIndex)
        {
            fixed (byte* ptr = &array[startIndex])
            {
                *(UInt32*)ptr = value;
            }
        }

        /// <summary>
        /// Copy Int64 bytes direct into buffer
        /// </summary>
        public unsafe static void ToBytes(this UInt64 value, byte[] array, int startIndex)
        {
            fixed (byte* ptr = &array[startIndex])
            {
                *(UInt64*)ptr = value;
            }
        }

        /// <summary>
        /// Copy Single bytes direct into buffer
        /// </summary>
        public unsafe static void ToBytes(this Single value, byte[] array, int startIndex)
        {
            fixed (byte* ptr = &array[startIndex])
            {
                *(Single*)ptr = value;
            }
        }

        /// <summary>
        /// Copy Double bytes direct into buffer
        /// </summary>
        public unsafe static void ToBytes(this Double value, byte[] array, int startIndex)
        {
            fixed (byte* ptr = &array[startIndex])
            {
                *(Double*)ptr = value;
            }
        }

        #endregion
    }
}