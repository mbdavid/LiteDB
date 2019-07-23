using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;
using System.Threading;
using LiteDB.Tests;

namespace LiteDB.Internals
{
    [TestClass]
    public class Aes_Tests
    {
        [TestMethod]
        public void Encrypt_Decrypt_Stream()
        {
            using (var media = new MemoryStream())
            using (var crypto = new AesStream("abc", media))
            {
                var input0 = new byte[8192];
                var input1 = new byte[8192];
                var input2 = new byte[8192];

                var output0 = new byte[8192];
                var output1 = new byte[8192];
                var output2 = new byte[8192];

                input0.Fill(100, 0, 8192);
                input1.Fill(101, 0, 8192);
                input2.Fill(102, 0, 8192);

                // write 0, 2, 1 but in order 0, 1, 2
                crypto.Position = 0 * 8192;
                crypto.Write(input0, 0, 8192);

                crypto.Position = 2 * 8192;
                crypto.Write(input2, 0, 8192);

                crypto.Position = 1 * 8192;
                crypto.Write(input1, 0, 8192);

                // read encrypted data
                media.Position = 0;
                media.Read(output0, 0, 8192);
                media.Read(output1, 0, 8192);
                media.Read(output2, 0, 8192);
                
                Assert.IsFalse(output0.All(x => x == 100));
                Assert.IsFalse(output1.All(x => x == 101));
                Assert.IsFalse(output2.All(x => x == 102));

                // read decrypted data
                crypto.Position = 0 * 8192;
                crypto.Read(output0, 0, 8192);

                crypto.Position = 2 * 8192;
                crypto.Read(output2, 0, 8192);

                crypto.Position = 1 * 8192;
                crypto.Read(output1, 0, 8192);

                Assert.IsTrue(output0.All(x => x == 100));
                Assert.IsTrue(output1.All(x => x == 101));
                Assert.IsTrue(output2.All(x => x == 102));


            }

        }
    }
}