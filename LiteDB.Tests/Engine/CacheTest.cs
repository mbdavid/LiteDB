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
    public class CacheTest
    {
        const int N = 60000;

        [TestMethod]
        public void CacheCheckpoint_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            { 
                var log = new StringBuilder();
                db.Log.Level = Logger.CACHE;
                db.Log.Logging += (s) => log.AppendLine(s);

                // insert basic N documents
                db.Insert("col", GetDocs(N));

                Assert.IsTrue(log.ToString().Contains("checkpoint"));
            }
        }

        [TestMethod]
        public void Recovery_Test()
        {
            using (var file = new TempFile())
            {
                // init with N docs
                using (var db = new LiteEngine(file.Filename))
                {
                    db.EnsureIndex("col", "type");

                    // insert N docs with type=1
                    db.Insert("col", GetDocs(N, 1, false));

                    // checks count
                    Assert.AreEqual(N, db.Count("col", Query.EQ("type", 1)));
                }

                // re-open and type update
                using (var db = new LiteEngine(file.Filename))
                {
                    var log = new StringBuilder();
                    db.Log.Level = Logger.CACHE;
                    db.Log.Logging += (s) => log.AppendLine(s);

                    try
                    {
                        // try update all to "type=2"
                        // but throws before finish
                        db.Update("col", GetDocs(N, 2, false));
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("Try Recovery!")) Assert.Fail(ex.Message);
                    }

                    // checks if cache had a checkpoint
                    Assert.IsTrue(log.ToString().Contains("checkpoint"));

                    // re-check if all docs will be type=1
                    Assert.AreEqual(0, db.Count("col", Query.EQ("type", 1)));
                    Assert.AreEqual(N, db.Count("col", Query.EQ("type", 2)));
                }

                // re-open datafile the be sure contains only type=1
                using (var db = new LiteEngine(file.Filename))
                {
                    Assert.AreEqual(0, db.Count("col", Query.EQ("type", 1)));
                    Assert.AreEqual(N, db.Count("col", Query.EQ("type", 2)));
                }
            }
        }

        private IEnumerable<BsonDocument> GetDocs(int count, int type = 1, bool throwAtEnd = false)
        {
            for(var i = 0; i < count; i++)
            {
                yield return new BsonDocument
                {
                    { "_id", i },
                    { "name", Guid.NewGuid().ToString() },
                    { "type", type },
                    { "lorem", TempFile.LoremIpsum(3, 5, 2, 3, 3) }
                };
            }

            // force throw exception during big insert
            if (throwAtEnd)
            {
                throw new Exception("Try Recovery!");
            }
        }
    }
}