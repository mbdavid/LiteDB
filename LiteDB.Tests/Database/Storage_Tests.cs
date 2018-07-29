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
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace LiteDB.Tests.Database
{
    [TestClass]
    public class Storage_Tests
    {
        private Random _rnd = new Random();
        private byte[] _smallFile;
        private byte[] _bigFile;
        private string _smallHash;
        private string _bigHash;

        [TestInitialize]
        public void Init()
        {
            _smallFile = new byte[_rnd.Next(100000, 200000)];
            _bigFile = new byte[_rnd.Next(400000, 600000)];

            _rnd.NextBytes(_smallFile);
            _rnd.NextBytes(_bigFile);

            _smallHash = this.HashFile(_smallFile);
            _bigHash = this.HashFile(_bigFile);

        }

        [TestMethod]
        public void Storage_Upload()
        {
            //using (var f = new TempFile())
            //using (var db = new LiteDatabase(f.Filename))
            using (var db = new LiteDatabase(@"c:\temp\file.db"))
            {
                var fs = db.GetStorage<int>("_files", "_chunks");

                var small = fs.Upload(10, "photo_small.png", new MemoryStream(_smallFile));
                var big = fs.Upload(100, "photo_big.png", new MemoryStream(_bigFile));

                Assert.AreEqual(small.Length, _smallFile.Length);
                Assert.AreEqual(big.Length, _bigFile.Length);

                var f0 = fs.Find(x => x.Filename == "photo_small.png").First();
                var f1 = fs.Find(x => x.Filename == "photo_big.png").First();

                Assert.AreEqual(_smallHash, this.HashFile(f0.OpenRead()));
                Assert.AreEqual(_bigHash, this.HashFile(f1.OpenRead()));

                // now replace small content with big-content
                var repl = fs.Upload(10, "new_photo.jpg", new MemoryStream(_bigFile));

                Assert.IsTrue(fs.Exists(10));

                var nrepl = fs.FindById(10);

                Assert.AreEqual(repl.Chunks, nrepl.Chunks);

                // update metadata
                fs.SetMetadata(100, new BsonDocument { ["x"] = 100 });
            }
        }

        private string HashFile(Stream stream)
        {
            var m = new MemoryStream();
            stream.CopyTo(m);
            return this.HashFile(m.ToArray());
        }

        private string HashFile(byte[] input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(input);
                return Convert.ToBase64String(bytes);
            }
        }
    }
}