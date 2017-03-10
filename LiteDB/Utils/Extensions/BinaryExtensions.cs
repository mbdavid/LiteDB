using System;

namespace LiteDB
{
    internal static class BinaryExtensions
    {
        // https://code.google.com/p/freshbooks-api/source/browse/depend/NClassify.Generator/content/ByteArray.cs?r=bbb6c13ec7a01eae082796550f1ddc05f61694b8
        public static int BinaryCompareTo(this byte[] lh, byte[] rh)
        {
            if (lh == null) return rh == null ? 0 : -1;
            if (rh == null) return 1;

            var result = 0;
            var i = 0;
            var stop = Math.Min(lh.Length, rh.Length);

            for (; 0 == result && i < stop; i++)
                result = lh[i].CompareTo(rh[i]);

            if (result != 0) return result < 0 ? -1 : 1;
            if (i == lh.Length) return i == rh.Length ? 0 : -1;
            return 1;
        }
    }
}