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

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Crypto_Tests
    {
        [TestMethod]
        public void Plain_Datafile()
        {
            var data = new MemoryStream();
            var log = new MemoryStream();

            var settings = new EngineSettings { DataStream = data, LogStream = log };

            using (var e = new LiteEngine(settings))
            {
                this.CreateDatabase(e);

                // find string inside stream
                var dataStr = Encoding.UTF8.GetString(data.ToArray());

                // plain datafile will found strings 
                Assert.IsTrue(dataStr.Contains("mycol"));
                Assert.IsTrue(dataStr.Contains("Mauricio"));

                // plain datafile will consume only 4 pages: 1 header, 1 collection, 1 data e 1 index
                Assert.AreEqual(4, data.Length / 8192);
            }
        }

        [TestMethod]
        public void Crypto_Datafile()
        {
            var data = new MemoryStream();
            var log = new MemoryStream();

            var settings = new EngineSettings { DataStream = data, LogStream = log, Password = "abc" };

            using (var e = new LiteEngine(settings))
            {
                this.CreateDatabase(e);

                // find string inside stream
                var dataStr = Encoding.UTF8.GetString(data.ToArray());

                // encrypted datafile will not found plain strings
                Assert.IsFalse(dataStr.Contains("mycol"));
                Assert.IsFalse(dataStr.Contains("Mauricio"));

                // but document exists!
                var doc = e.Find("mycol", "_id=1").First();

                Assert.AreEqual("Mauricio", doc["name"].AsString);

                // encrypted datafile will consume 5 pages: 1 salt page, 1 header, 1 collection, 1 data e 1 index
                Assert.AreEqual(5, data.Length / 8192);
            }
        }

        private void CreateDatabase(LiteEngine engine)
        {
            engine.UserVersion = 123;

            engine.Insert("mycol", new[] {
                new BsonDocument
                {
                    ["_id"] = 1,
                    ["name"] = "Mauricio"
                }
            }, BsonAutoId.Int32);

            // do checkpoint to use only data file
            engine.Checkpoint();
        }
    }
}