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
        private static object _lock;
        private static ArrayPool<byte> _bytePool;

        static BufferPool()
        {
            _lock = new object();
            _bytePool = new ArrayPool<byte>();
        }
        
        public static byte[] Rent(int count)
        {
            lock (_lock)
            {
                return _bytePool.Rent(count);
            }
        }

        public static void Return(byte[] buffer)
        {
            lock (_lock)
            {
                _bytePool.Return(buffer);
            }
        }

        public static byte[] RentPageSizeBuff()
        {
            // TODO
            // Can not use Rent For PAGE_SIZE. because. this buffer store in BufferSlice and use, after return
            // May be create Buffer Slice like Disposable and create wrapper for pool array
            return new byte[Constants.PAGE_SIZE];
        }

        public static void ReturnPageSizeBuff(byte[] buff)
        {
        }
    }
}