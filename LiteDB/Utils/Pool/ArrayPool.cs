using System;
using System.Runtime.CompilerServices;
using static LiteDB.Constants;

namespace LiteDB
{
    public class ArrayPool<T>
    {
        private static readonly T[] Emptry = new T[0];

        private const int SLOT_COUNT = 8;

        private readonly SlotBuff[] _buckets;

        public ArrayPool()
        {
            _buckets = new SlotBuff[BucketHelper.BucketCount];
            for (var i = 0; i < BucketHelper.BucketCount; ++i)
            {
                var maxSlotSize = BucketHelper.GetMaxSizeForBucket(i);

                var slotBuff = new SlotBuff();
                _buckets[i] = slotBuff;

                for (var j = 0; j < SLOT_COUNT; ++j)
                {
                    if (!slotBuff.TryPush(new T[maxSlotSize]))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        public T[] Rent(int minSize)
        {
            if (minSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minSize));
            }

            if (minSize == 0)
            {
                return Emptry;
            }

            var bucketIdx = BucketHelper.GetBucketIndex(minSize);
            if (bucketIdx < 0)
            {
                return new T[minSize];
            }

            var buff = _buckets[bucketIdx];
            var returnBuff = buff.TryPop();

            if (returnBuff != null)
            {
                return returnBuff;
            }

            return new T[minSize];
        }

        public void Return(T[] buff)
        {
            if (buff == null)
            {
                throw new ArgumentNullException(nameof(buff));
            }

            if (buff.Length == 0)
            {
                return;
            }

            var bucketIndex = BucketHelper.GetBucketIndex(buff.Length);
            if (bucketIndex < 0)
                return;

            var buffer = _buckets[bucketIndex];
            buffer.TryPush(buff);
        }

        private sealed class SlotBuff
        {
            private T[][] _buff = new T[SLOT_COUNT][];
            private int _size;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryPush(T[] item)
            {
                if (_size >= SLOT_COUNT)
                    return false;

                _buff[_size++] = item;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T[] TryPop()
            {
                if (_size <= 0)
                    return null;

                var arr = _buff[--_size];
                _buff[_size] = null;
                return arr;
            }
        }
    }
}