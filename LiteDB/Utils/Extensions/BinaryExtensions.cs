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

        //https://stackoverflow.com/questions/43289/comparing-two-byte-arrays-in-net
        // need: <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        //
        //static unsafe bool UnsafeCompare(byte[] a1, byte[] a2)
        //{
        //    if (a1 == a2) return true;
        //    if (a1 == null || a2 == null || a1.Length != a2.Length)
        //        return false;
        //    fixed (byte* p1 = a1, p2 = a2)
        //    {
        //        byte* x1 = p1, x2 = p2;
        //        int l = a1.Length;
        //        for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
        //            if (*((long*)x1) != *((long*)x2)) return false;
        //        if ((l & 4) != 0) { if (*((int*)x1) != *((int*)x2)) return false; x1 += 4; x2 += 4; }
        //        if ((l & 2) != 0) { if (*((short*)x1) != *((short*)x2)) return false; x1 += 2; x2 += 2; }
        //        if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2)) return false;
        //        return true;
        //    }
        //}
    }
}