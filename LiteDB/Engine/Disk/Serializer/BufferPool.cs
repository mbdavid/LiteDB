using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement similar as ArrayPool for byte array
    /// </summary>
    internal class BufferPool
    {
        public static byte[] Rent(int count)
        {
            return new byte[count];
        }

        public static void Return(byte[] buffer)
        {
            // not implemented!
        }
    }
}