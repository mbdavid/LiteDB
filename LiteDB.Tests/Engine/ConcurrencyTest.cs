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
    public class ConcurrencyTest
    {
        [TestMethod]
        public void InsertTask_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.EnsureIndex("col", "thread");

                // insert 5000 x thread=1
                var ta = Task.Factory.StartNew(() =>
                {
                    for(var i = 0; i < 5000; i++)
                        db.Insert("col", new BsonDocument { { "thread", 1 } });
                });

                // insert 4000 x thread=2
                var tb = Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < 4000; i++)
                        db.Insert("col", new BsonDocument { { "thread", 2 } });
                });

                Task.WaitAll(ta, tb);

                Assert.AreEqual(5000, db.Count("col", Query.EQ("thread", 1)));
                Assert.AreEqual(4000, db.Count("col", Query.EQ("thread", 2)));
            }
        }

        [TestMethod]
        public void InsertUpdateTask_Test()
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
                    while(i < N)
                    {
                        var doc = new BsonDocument
                        {
                            { "_id", i },
                            { "updated", true },
                            { "name", TempFile.LoremIpsum(5, 10, 1, 5, 1) }
                        };

                        if(db.Update("col", doc)) i++;
                    }
                });

                Task.WaitAll(ta, tb);

                Assert.AreEqual(N, db.Count("col", Query.EQ("updated", true)));
            }
        }
    }
}