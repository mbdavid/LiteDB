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
    public class MultiThreadTest
    {
        [TestMethod]
        public void Thread_Insert_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.EnsureIndex("col", "thread");

                // insert 1000 x thread=1
                var ta = Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < 1000; i++)
                        db.Insert("col", new BsonDocument { { "thread", 1 } });
                });

                // insert 700 x thread=2
                var tb = Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < 700; i++)
                        db.Insert("col", new BsonDocument { { "thread", 2 } });
                });

                Task.WaitAll(ta, tb);

                Assert.AreEqual(1000, db.Count("col", Query.EQ("thread", 1)));
                Assert.AreEqual(700, db.Count("col", Query.EQ("thread", 2)));
            }
        }

        [TestMethod]
        public void Thread_InsertUpdate_Test()
        {
            const int N = 3000;

            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.EnsureIndex("col", "updated");

                Assert.AreEqual(0, db.Count("col", Query.EQ("updated", true)));

                // insert basic document
                var ta = Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < N; i++)
                    {
                        var doc = new BsonDocument { { "_id", i } };

                        db.Insert("col", doc);
                    }
                });

                // update _id=N
                var tb = Task.Factory.StartNew(() =>
                {
                    var i = 0;
                    while (i < N)
                    {
                        var doc = new BsonDocument
                        {
                            { "_id", i },
                            { "updated", true },
                            { "name", TempFile.LoremIpsum(5, 10, 1, 5, 1) }
                        };

                        if (db.Update("col", doc)) i++;
                    }
                });

                Task.WaitAll(ta, tb);

                Assert.AreEqual(N, db.Count("col", Query.EQ("updated", true)));
            }
        }

        [TestMethod]
        public void Thread_InsertQuery_Test()
        {
            const int N = 3000;
            var running = true;

            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.Insert("col", new BsonDocument());

                // insert basic document
                var ta = Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < N; i++)
                    {
                        var doc = new BsonDocument { { "_id", i } };

                        db.Insert("col", doc);
                    }
                    running = false;
                });

                // query while insert
                var tb = Task.Factory.StartNew(() =>
                {
                    while (running)
                    {
                        db.Find("col", Query.All()).ToList();
                    }
                });

                Task.WaitAll(ta, tb);

                Assert.AreEqual(N + 1, db.Count("col", Query.All()));
            }
        }

        [TestMethod]
        public void Thread_UserVersionInc_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                Parallel.For(1, 3001, (i) =>
                {
                    // concurrency must be locked
                    lock(db)
                    {
                        db.UserVersion = (ushort)(db.UserVersion + 1);
                    }
                });

                Assert.AreEqual(3000, db.UserVersion);
            }
        }

        [TestMethod]
        public void Thread_Transaction_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                // insert first document
                db.Insert("col", new BsonDocument
                {
                    { "_id", 1 },
                    { "count", 1 }
                });

                // use parallel 
                Parallel.For(1, 10000, (i) =>
                {
                    lock (db)
                    {
                        var doc = db.Find("col", Query.EQ("_id", 1)).Single();
                        doc["count"] = doc["count"].AsInt32 + 1;
                        db.Update("col", doc);
                    }
                });

                Assert.AreEqual(10000, db.Find("col", Query.EQ("_id", 1)).Single()["count"].AsInt32);
            }
        }
    }
}