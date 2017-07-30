using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LiteDB.Tests.Concurrency
{
    [TestClass]
    public class Locker_Tests
    {
        [TestMethod, TestCategory("Concurrency")]
        public void Loop_With_Update_Test()
        {
            using (var tmp = new TempFile())
            {
                // initialize database with 4
                using (var db = new LiteEngine(tmp.Filename))
                {
                    db.Insert("col", new BsonDocument { { "Number", 1 } });
                    db.Insert("col", new BsonDocument { { "Number", 2 } });
                    db.Insert("col", new BsonDocument { { "Number", 3 } });
                    db.Insert("col", new BsonDocument { { "Number", 4 } });
                }

                using (var db = new LiteEngine(tmp.Filename))
                {
                    // locker here must be unlocked
                    Assert.AreEqual(LockState.Unlocked, db.Locker.ThreadState);

                    foreach (var doc in db.Find("col", Query.All()))
                    {
                        // locker here must be read
                        Assert.AreEqual(LockState.Read, db.Locker.ThreadState);

                        doc["Name"] = "John";

                        // inside this update, locker must be in write
                        db.Update("col", doc);

                        // but after update must back to read
                        Assert.AreEqual(LockState.Read, db.Locker.ThreadState);
                    }

                    // back do unlock
                    Assert.AreEqual(LockState.Unlocked, db.Locker.ThreadState);

                    db.EnsureIndex("col", "Name");
                    var all = db.Find("col", Query.EQ("Name", "John"));

                    Assert.AreEqual(4, all.Count());
                }
            }
        }
    }
}