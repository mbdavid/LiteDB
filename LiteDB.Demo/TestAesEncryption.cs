using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class TestAesEncryption
    {
        public static void Run(Stopwatch sw)
        {
            var salt = AesEncryption.Salt(null);
            var buffer0 = new byte[8192];
            var buffer1 = new byte[8192];
            var orig = new byte[8192];

            for (var j = 0; j < 32; j++)
                for (var i = 0; i < 256; i++)
                {
                    buffer0[(j * 256) + i] = (byte)i;
                    buffer1[(j * 256) + i] = (byte)i;
                    orig[(j * 256) + i] = (byte)i;
                }

            var slice0 = new ArraySlice<byte>(buffer0, 0, 8192);
            var slice1 = new ArraySlice<byte>(buffer0, 0, 8192);

            var mem = new MemoryStream();


            using (var aes = new AesEncryption("abc", salt))
            {
                aes.Encrypt(slice0, mem);

                mem.Position = 0;

                //aes.Decrypt(slice);

            }

            using (var aes2 = new AesEncryption("abc", salt))
            {
                aes2.Decrypt(mem, slice1);
            }


            Console.WriteLine("CompareTo: " + slice0.Array.BinaryCompareTo(slice1.Array)); // 0 = equals
            Console.WriteLine("CompareTo: " + slice0.Array.BinaryCompareTo(orig)); // 0 = equals
            Console.WriteLine("CompareTo: " + slice1.Array.BinaryCompareTo(orig)); // 0 = equals

        }
    }

}
