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
    public class CheckPointTest
    {
        const int N = 60000;

        [TestMethod]
        public void Checkpoint_Insert_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            { 
                var log = new StringBuilder();
                db.Log.Level = Logger.CACHE;
                db.Log.Logging += (s) => log.AppendLine(s);

                // insert basic N documents
                db.Insert("col", GetDocs(1, N));

                Assert.IsTrue(log.ToString().Contains("checkpoint"));
            }
        }

        [TestMethod]
        public void Checkpoint_Index_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                // insert basic N documents
                db.Insert("col", GetDocs(1, N));

                var log = new StringBuilder();
                db.Log.Level = Logger.CACHE;
                db.Log.Logging += (s) => log.AppendLine(s);

                // create an index in col
                db.EnsureIndex("col", "name");

                Assert.IsTrue(log.ToString().Contains("checkpoint"));

                Assert.AreEqual(N, db.Count("col", Query.All()));
            }
        }

        [TestMethod]
        public void Checkpoint_Recovery_Test()
        {
            using (var file = new TempFile())
            {
                // init with N docs with type=1
                using (var db = new LiteEngine(file.Filename))
                {
                    db.EnsureIndex("col", "type");
                    db.Insert("col", GetDocs(1, N, type: 1));
                    Assert.AreEqual(N, db.Count("col", Query.EQ("type", 1)));
                }

                // re-open and try update all docs to type=2
                using (var db = new LiteEngine(file.Filename))
                {
                    var log = new StringBuilder();
                    db.Log.Level = Logger.CACHE;
                    db.Log.Logging += (s) => log.AppendLine(s);

                    try
                    {
                        // try update all to "type=2"
                        // but throws exception before finish
                        db.Update("col", GetDocs(1, N, type: 2, throwAtEnd: true));
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("Try Recovery!")) Assert.Fail(ex.Message);
                    }

                    // checks if cache had a checkpoint
                    Assert.IsTrue(log.ToString().Contains("checkpoint"));

                    // re-check if all docs will be type=1
                    Assert.AreEqual(N, db.Count("col", Query.EQ("type", 1)));
                    Assert.AreEqual(0, db.Count("col", Query.EQ("type", 2)));
                }

                // re-open datafile the be sure contains only type=1
                using (var db = new LiteEngine(file.Filename))
                {
                    Assert.AreEqual(N, db.Count("col", Query.EQ("type", 1)));
                    Assert.AreEqual(0, db.Count("col", Query.EQ("type", 2)));
                }
            }
        }

        [TestMethod]
        public void Checkpoint_TransactionRecovery_Test()
        {
            using (var file = new TempFile())
            {
                using (var db = new LiteEngine(new FileDiskService(file.Filename)))
                {
                    var log = new StringBuilder();
                    db.Log.Level = Logger.CACHE;
                    db.Log.Logging += (s) => log.AppendLine(s);

                    // initialize my "col" with 1000 docs without transaction
                    db.Insert("col", GetDocs(1, 1000));

                    // commit now for intialize new transaction
                    db.Commit();

                    // insert a lot of docs inside a single collection (will do checkpoint in disk)
                    db.Insert("col", GetDocs(1001, N));

                    // update all documents
                    db.Update("col", GetDocs(1, N));

                    // create new index
                    db.EnsureIndex("col", "type");

                    // checks if cache had a checkpoint
                    Assert.IsTrue(log.ToString().Contains("checkpoint"));

                    // datafile must be big (because checkpoint expand file)
                    Assert.IsTrue(file.Size > 30 * 1024 * 1024); // in MB\

                    // delete all docs > 1000
                    db.Delete("col", Query.GT("_id", 1000));

                    db.DropIndex("col", "type");

                    // let's rollback everything 
                    db.Rollback();

                    // be sure cache are empty
                    Assert.AreEqual(0, db.CacheUsed);

                    // datafile must returns to original size (less than 1.5MB for 1000 docs)
                    Assert.IsTrue(file.Size < 1.5 * 1024 * 1024); // in MB

                    // test in my only doc exits
                    Assert.AreEqual(1000, db.Count("col", Query.All()));
                    Assert.AreEqual(1000, db.Count("col", null));

                    // test indexes (must have only _id index)
                    Assert.AreEqual(1, db.GetIndexes("col").Count());
                }
            }
        }

        private IEnumerable<BsonDocument> GetDocs(int initial, int count, int type = 1, bool throwAtEnd = false)
        {
            for(var i = initial; i < initial + count; i++)
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