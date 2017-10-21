using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Shrink_Tests
    {
        [TestMethod]
        public void Shrink_After_DropCollection()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.InsertBulk("col", GetDocs(1, 10000));

                db.DropCollection("col");

                // full disk usage
                var size = file.Size;

                var r = db.Shrink();

                // only header page + reserved lock page
                Assert.AreEqual(8192, size - r);
            }
        }

        [TestMethod]
        public void Shrink_Large_Files()
        {
            // do some tests
            Action<LiteEngine> DoTest = (db) =>
            {
                Assert.AreEqual(1, db.Count("col", null));
                Assert.AreEqual(99, db.UserVersion);
                Assert.IsNotNull(db.GetIndexes("col").FirstOrDefault(x => x.Field == "name"));
                Assert.IsTrue(db.GetIndexes("col").FirstOrDefault(x => x.Field == "name").Unique);
            };

            using (var file = new TempFile())
            {
                using (var dbe = new LiteDatabase(file.Filename))
                {
                    var db = dbe.Engine;

                    db.UserVersion = 99;
                    db.EnsureIndex("col", "name", true);
                    db.Insert("col", GetDocs(1, 40000));
                    db.Delete("col", Query.GT("_id", 1)); // delete 29.999 docs

                    Assert.AreEqual(1, db.Count("col", null));

                    // file still large than 20mb (even with only 1 document)
                    Assert.IsTrue(file.Size > 20 * 1024 * 1024);

                    // reduce datafile (use temp disk) (from LiteDatabase)
                    dbe.Shrink();

                    // now file are small than 50kb
                    Assert.IsTrue(file.Size < 50 * 1024);

                    DoTest(db);
                }

                // re-open datafile to check if is ok
                using (var db = new LiteDatabase(file.Filename))
                {
                    // still 1 doc and 1 name unique index
                    DoTest(db.Engine);

                    // shrink again but now with password
                    var reduced = db.Shrink("abc123");

                    // file still same size (but now are encrypted)
                    Assert.AreEqual(0, reduced);

                    // still 1 doc and 1 name unique index
                    DoTest(db.Engine);
                }

                // re-open, again, but now with password
                using (var db = new LiteEngine(file.Filename, "abc123"))
                {
                    DoTest(db);

                    // now, remove password
                    db.Shrink();

                    // test again
                    DoTest(db);
                }
            }
        }

        private IEnumerable<BsonDocument> GetDocs(int initial, int count, int type = 1)
        {
            for (var i = initial; i < initial + count; i++)
            {
                yield return new BsonDocument
                {
                    { "_id", i },
                    { "name", Guid.NewGuid().ToString() },
                    { "first", "John" },
                    { "lorem", TempFile.LoremIpsum(3, 5, 2, 3, 3) }
                };
            }
        }
    }
}