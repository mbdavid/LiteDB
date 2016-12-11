using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using System.Linq;

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

                    // open datafile as readonly mode
                    Task.Factory.StartNew(() =>
                    {
                        using (var r = new LiteEngine(new FileDiskService(file.Filename, new FileOptions { FileMode = FileOpenMode.ReadOnly })))
                        {
                            var doc = r.Find("col", Query.EQ("_id", 1)).FirstOrDefault();

                            Assert.AreEqual(1, doc["_id"].AsInt32);

                            // do not support write operation
                            try
                            {
                                r.Insert("doc", new BsonDocument { { "_id", 2 } });

                                Assert.Fail("Do not accept write operation in readonly database");
                            }
                            catch (LiteException ex)
                            {
                                if (ex.ErrorCode != LiteException.READ_ONLY_DATABASE)
                                {
                                    Assert.Fail("Wrong exception");
                                }
                            }
                        }
                    }).Wait();

                    // try open second datafile in write mode (must throw exception)
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            // try open datafile as read/write
                            using (var rw = new LiteEngine(new FileDiskService(file.Filename, new FileOptions { Timeout = TimeSpan.FromSeconds(3) })))
                            {
                                Assert.Fail("Do not open this datafile");
                            }
                        }
                        catch (LiteException ex)
                        {
                            if (ex.ErrorCode != LiteException.LOCK_TIMEOUT)
                            {
                                Assert.Fail("Wrong exception");
                            }
                        }
                    }).Wait();
                }
            }
        }

        [TestMethod]
        public void ReadOnlyFirst_Test()
        {
            using (var file = new TempFile())
            {
                // just create file
                using (var c = new LiteEngine(file.Filename))
                {
                    c.Insert("col", new BsonDocument { { "_id", 1 } });
                }

                // here there is no open datafile

                // open as read-only
                using (var r = new LiteEngine(new FileDiskService(file.Filename, new FileOptions { FileMode = FileOpenMode.ReadOnly })))
                {
                    // just query
                    var d = r.Find("col", Query.EQ("_id", 1)).FirstOrDefault();
                    Assert.AreEqual(1, d["_id"].AsInt32);

                    // open database in read/write mode
                    Task.Factory.StartNew(() =>
                    {
                        using (var rw = new LiteEngine(file.Filename))
                        {
                            rw.Insert("col", new BsonDocument { { "_id", 2 } });
                        }
                    }).Wait();
                }
            }
        }
    }
}