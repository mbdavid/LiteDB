using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    class TestChunk
    {
        public static void Run()
        {
            var b = new ChunkStream(GetSource(), 20);
            var reader = new BinaryReader(b);

            var r0_1 = reader.ReadBytes(2);
            var r2_10 = reader.ReadBytes(9);
            var r11_12 = reader.ReadBytes(2);

            b.Seek(5, SeekOrigin.Current);

            var r14_15 = reader.ReadBytes(2);



        }

        static IEnumerable<byte[]> GetSource()
        {
            yield return new byte[] { 0, 1, 2, 3, 4 };
            yield return new byte[] { 5, 6, 7 };
            yield return new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 };
            yield return new byte[] { 16 };
            yield return new byte[] { 17 };
            yield return new byte[] { 18, 19 };

        }
    }
}
