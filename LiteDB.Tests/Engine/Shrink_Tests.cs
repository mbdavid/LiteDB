using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LiteDB.Engine;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Shrink_Tests
    {
        [TestMethod]
        public void Shrink_After_DropCollection()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.FileName))
            {
                db.Insert("col", DataGen.Zip());

                db.DropCollection("col");

                // full disk usage
                var size = file.Size;

                var r = db.Shrink();

                // only header page
                Assert.AreEqual(8192, size - r);
            }
        }

        [TestMethod]
        public void Shrink_Large_Files()
        {
            // do some tests
            Action<LiteEngine> DoTest = (db) =>
            {
                Assert.AreEqual(1, db.Count("col"));
                Assert.AreEqual(99, db.UserVersion);
            };

            using (var file = new TempFile())
            {
                using (var db = new LiteEngine(file.FileName))
                {
                    db.UserVersion = 99;
                    db.EnsureIndex("col", "city", false);

                    var inserted = db.Insert("col", DataGen.Zip()); // 29.353 docs
                    var deleted = db.DeleteMany("col", "_id != '01001'"); // delete 29.352 docs

                    Assert.AreEqual(29353, inserted);
                    Assert.AreEqual(29352, deleted);

                    Assert.AreEqual(1, db.Count("col"));

                    // must checkpoint
                    db.Checkpoint(false);

                    // file still large than 5mb (even with only 1 document)
                    Assert.IsTrue(file.Size > 5 * 1024 * 1024);

                    // reduce datafile (use temp disk) (from LiteDatabase)
                    var reduced = db.Shrink();

                    // now file are small than 50kb
                    Assert.IsTrue(file.Size < 50 * 1024);

                    DoTest(db);
                }

                // re-open and shrink again
                using (var db = new LiteEngine(file.FileName))
                {
                    DoTest(db);

                    db.Shrink();

                    DoTest(db);
                }
            }
        }
    }
}