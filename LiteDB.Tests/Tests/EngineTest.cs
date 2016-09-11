using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LiteDB.Tests
{
    [TestClass]
    public class EngineTest
    {
        [TestMethod]
        public void Engine_Insert_Test()
        {
            using (var file = new TempFile())
            {
                using (var db = new LiteEngine(file.Filename))
                {
                    db.Insert("col1", new BsonDocument().Add("_id", 1).Add("name", "John"));
                    db.Insert("col1", new BsonDocument().Add("_id", 2).Add("name", "Doe"));
                }

                using (var db = new LiteEngine(file.Filename))
                {
                    var john = db.Find("col1", Query.EQ("_id", 1)).FirstOrDefault();
                    var doe = db.Find("col1", Query.EQ("_id", 2)).FirstOrDefault();

                    Assert.AreEqual("John", john["name"].AsString);
                    Assert.AreEqual("Doe", doe["name"].AsString);
                }
            }
        }

        [TestMethod]
        public void Engine_Task_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.EnsureIndex("col", "thread", new IndexOptions());

                // insert 5000 x thread=1
                var ta = Task.Factory.StartNew(() =>
                {
                    for(var i = 0; i < 5000; i++)
                        db.Insert("col", new BsonDocument().Add("thread", 1));
                });

                // insert 4000 x thread=2
                var tb = Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < 4000; i++)
                        db.Insert("col", new BsonDocument().Add("thread", 2));
                });

                Task.WaitAll(ta, tb);

                Assert.AreEqual(5000, db.Count("col", Query.EQ("thread", 1)));
                Assert.AreEqual(4000, db.Count("col", Query.EQ("thread", 2)));
            }
        }
    }
}