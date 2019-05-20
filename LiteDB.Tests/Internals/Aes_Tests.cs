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
            var salt = AesEncryption.NewSalt();
            var aes = new AesEncryption("abc", salt);

            // encrypt
            var media = new MemoryStream();
            var memory = new byte[4 * 8192];
            var input = new BufferSlice(memory, 0, 8192);
            var output = new BufferSlice(memory, 0, 8192);

            input.Fill(99);

            aes.Encrypt(input, media);

            Assert.IsFalse(media.ToArray().All(x => x == 99));
            Assert.IsFalse(media.ToArray().All(x => x == 0));

            // decrypt
            media.Position = 0;

            aes.Decrypt(media, output);

            Assert.IsTrue(output.All(99));

            // decrypt with wrong password
            var aes1 = new AesEncryption("abC", salt);

            media.Position = 0;

            aes1.Decrypt(media, output);

            Assert.IsFalse(output.All(99));

        }

        [TestMethod]
        public void Encrypt_Decrypt_Multi_Pages()
        {
            var salt = AesEncryption.NewSalt();
            var aes = new AesEncryption("abc", salt);

            // encrypt
            var pages = 4;
            var media = new MemoryStream();
            var memory = new byte[pages * 8192];

            for(var i = 0; i < pages; i++)
            {
                var input = new BufferSlice(memory, i * 8192, 8192);

                input.Fill((byte)(99 + i));

                aes.Encrypt(input, media);
            }

            media.Position = 0;

            // decrypt
            for (var i = 0; i < pages; i++)
            {
                var output = new BufferSlice(memory, i * 8192, 8192);

                aes.Decrypt(media, output);

                Assert.IsTrue(output.All((byte)(99 + i)));
            }
        }
    }
}