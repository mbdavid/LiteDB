using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class ReadOnlyTest
    {
        [TestMethod]
        public void ReadOnly_Test()
        {
            using (var file = new TempFile())
            {
                // open database with read/write
                using (var db = new LiteEngine(file.Filename))
                {
                    // here datafile are in Read/Write
                    db.Insert("col", new BsonDocument { { "_id", 1 } });
                }

                using (var r = new LiteEngine(new FileDiskService(file.Filename, new FileOptions { FileMode = FileMode.ReadOnly })))
                {
                    var doc = r.Find("col", Query.EQ("_id", 1)).FirstOrDefault();

                    Assert.AreEqual(1, doc["_id"].AsInt32);

                    // do not support write operation
                    try
                    {
                        r.Insert("doc", new BsonDocument { { "_id", 2 } });

                        Assert.Fail("Do not accept write operation in readonly database");
                    }
                    catch (NotSupportedException)
                    {
                    }
                }
            }
        }
    }
}