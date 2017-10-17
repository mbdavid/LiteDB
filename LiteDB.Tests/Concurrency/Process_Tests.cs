using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Concurrency
{
    [TestClass]
    public class Process_Tests
    {
        [TestMethod]
        public void Process_Multi_Insert()
        {
            using (var file = new TempFile())
            {
                using (var dbA = new LiteEngine(file.Filename))
                using (var dbB = new LiteEngine(file.Filename))
                {
                    dbA.EnsureIndex("col", "process", false);

                    // insert 1000 x instance=1
                    var ta = Task.Factory.StartNew(() =>
                    {
                      for (var i = 0; i < 1000; i++)
                      {
                          dbA.Insert("col", new BsonDocument { { "process", 1 } });
                      }
                    });

                    // insert 700 x instance=2
                    var tb = Task.Factory.StartNew(() =>
                    {
                      for (var i = 0; i < 700; i++)
                      {
                          dbB.Insert("col", new BsonDocument { { "process", 2 } });
                      }
                    });

                    Task.WaitAll(ta, tb);

                    Assert.AreEqual(1000, dbA.Count("col", Query.EQ("process", 1)));
                    Assert.AreEqual(700, dbA.Count("col", Query.EQ("process", 2)));

                    Assert.AreEqual(1000, dbB.Count("col", Query.EQ("process", 1)));
                    Assert.AreEqual(700, dbB.Count("col", Query.EQ("process", 2)));

                }
            }
        }

        [TestMethod]
        public void Process_Insert_Count()
        {
            using (var file = new TempFile())
            {
                using (var dbA = new LiteEngine(file.Filename))
                using (var dbB = new LiteEngine(file.Filename))
                {
                    dbA.EnsureIndex("col", "process", false);

                    // insert 1000 x instance=1
                    var ta = Task.Factory.StartNew(() =>
                    {
                        for (var i = 0; i < 1000; i++)
                        {
                            dbA.Insert("col", new BsonDocument { { "process", 1 } });
                        }
                    });

                    // keep querying until found 1000 docs
                    var tb = Task.Factory.StartNew(() =>
                    {
                        var count = 0L;

                        while (count < 1000)
                        {
                            // force query all rows
                            count = dbB.Count("col", Query.EQ("process", 1));

                            Task.Delay(50).Wait();
                        }
                    });

                    Task.WaitAll(ta, tb);

                    Assert.AreEqual(1000, dbA.Count("col", Query.EQ("process", 1)));
                    Assert.AreEqual(1000, dbB.Count("col", Query.EQ("process", 1)));

                }
            }
        }

        [TestMethod]
        public void Process_Insert_Delete()
        {
            using (var file = new TempFile())
            {
                using (var dbA = new LiteEngine(file.Filename))
                using (var dbB = new LiteEngine(file.Filename))
                {
                    dbA.EnsureIndex("col", "process", false);

                    // insert 1000 x instance=1
                    var ta = Task.Factory.StartNew(() =>
                    {
                        for (var i = 0; i < 1000; i++)
                        {
                            dbA.Insert("col", new BsonDocument { { "process", 1 } });
                        }
                    });

                    // keeping delete all
                    var tb = Task.Factory.StartNew(() =>
                    {
                        // while before starts insert
                        while (dbB.Count("col", Query.EQ("process", 1)) == 0)
                        {
                            Task.Delay(50).Wait();
                        }

                        // while until has docs
                        while (dbB.Count("col", Query.EQ("process", 1)) > 0)
                        {
                            dbB.Delete("col", Query.All());
                            Task.Delay(50).Wait();
                        }
                    });

                    Task.WaitAll(ta, tb);

                    Assert.AreEqual(0, dbA.Count("col", Query.EQ("process", 1)));
                    Assert.AreEqual(0, dbB.Count("col", Query.EQ("process", 1)));

                }
            }
        }
    }
}