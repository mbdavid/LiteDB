//using LiteDB;
//using LiteDB.Engine;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace LiteDB.Tests.Internals
//{
//    public class TestAesEncryption
//    {
//        static string PATH = @"D:\memory-file.db";

//        public static void Run(Stopwatch sw)
//        {
//            var salt = AesEncryption.NewSalt();
//            var buffer0 = new byte[8192];
//            var buffer1 = new byte[8192];
//            var orig = new byte[8192];

//            for (var j = 0; j < 32; j++)
//                for (var i = 0; i < 256; i++)
//                {
//                    buffer0[(j * 256) + i] = (byte)i;
//                    buffer1[(j * 256) + i] = (byte)i;
//                    orig[(j * 256) + i] = (byte)i;
//                }

//            var slice0 = new BufferSlice(buffer0, 0, 8192);
//            var slice1 = new BufferSlice(buffer0, 0, 8192);

//            var mem = new MemoryStream();


//            using (var aes = new AesEncryption("abc", salt))
//            {
//                aes.Encrypt(slice0, mem);

//                mem.Position = 0;

//                //aes.Decrypt(slice);

//            }

//            using (var aes2 = new AesEncryption("abc", salt))
//            {
//                aes2.Decrypt(mem, slice1);
//            }


//            Console.WriteLine("CompareTo: " + slice0.Array.BinaryCompareTo(slice1.Array)); // 0 = equals
//            Console.WriteLine("CompareTo: " + slice0.Array.BinaryCompareTo(orig)); // 0 = equals
//            Console.WriteLine("CompareTo: " + slice1.Array.BinaryCompareTo(orig)); // 0 = equals

//        }

//        public static void CreateEncryptedFile(Stopwatch sw)
//        {
//            File.Delete(PATH);

//            var factory = new FileStreamFactory(PATH, DbFileMode.Datafile, false);
//            var pool = new StreamPool(factory);
//            var aes = AesEncryption.CreateAes(pool, "abc");
//            var file = new MemoryFile(pool, aes);
//            var reader = file.GetReader(true);

//            var buffer0 = reader.NewPage(true);
//            var p0 = new HeaderPage(buffer0, 0);

//            p0.UserVersion = 99;
//            p0.NextPageID = 25;
//            p0.PrevPageID = 26;
//            //p0.UpdateCollections(new TransactionPages { NewCollections = new Dictionary<string, uint>() { ["myCol1"] = 1234 } });

//            p0.GetBuffer(true);

//            var p1 = reader.NewPage(true);
//            p1.Array.Fill((byte)25, p1.Offset, p1.Count);


//            file.WriteAsync(new[] { buffer0, p1 });

//            reader.Dispose();

//            file.Dispose();
//            pool.Dispose();
//        }

//        public static void ReadEncryptedFile(Stopwatch sw)
//        {
//            var factory = new FileStreamFactory(PATH, DbFileMode.Datafile, false);
//            var pool = new StreamPool(factory);
//            var aes = AesEncryption.CreateAes(pool, "abc");
//            var file = new MemoryFile(pool, aes);
//            var reader = file.GetReader(true);

//            var p0 = reader.GetPage(0, true);
//            var p1 = reader.GetPage(8192, true);

//            var h = new HeaderPage(p0);



//            reader.Dispose();

//            file.Dispose();
//            pool.Dispose();
//        }


//    }

//}
