using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests
{
    [TestClass]
    public class StorageStreamTest
    {
        [TestMethod]
        public void Storage_ReadWriteStream_Test()
        {
            var HELLO1 = "Hello World LiteDB 1 ".PadRight(300000, '-') + "\nEND";
            var HELLO2 = "Hello World LiteDB 2 - END";

            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                var sto = new LiteStorage(db);

                // insert HELLO1 file content
                using (var stream = sto.OpenWrite("f1", "f1.txt"))
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        sw.Write(HELLO1);
                    }
                }

                // test if was updated Length in _files collection
                var doc = db.Find("_files", Query.EQ("_id", "f1")).Single();

                Assert.AreEqual(HELLO1.Length, doc["length"].AsInt32);

                using (var stream = sto.OpenRead("f1"))
                {
                    var sr = new StreamReader(stream);
                    var hello = sr.ReadToEnd();

                    Assert.AreEqual(HELLO1, hello);
                }

                // updating to HELLO2 content same file id
                using (var stream = sto.OpenWrite("f1", "f1.txt"))
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        sw.Write(HELLO2);
                    }
                }
                using (var stream = sto.OpenRead("f1"))
                {
                    var sr = new StreamReader(stream);
                    var hello = sr.ReadToEnd();

                    Assert.AreEqual(HELLO2, hello);
                }
            }
        }
    }
}