using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
    public class StreamTestsClass
    {
        public int Id { get; set; }
        public String Value { get; set; }
    }

    [TestClass]
    public class StreamDbTest : TestBase
    {
        [TestMethod]
        public void FindBeforeInsertClosesTransaction()
        {
            MemoryStream ms = new MemoryStream();

            using (var db = new LiteDatabase(ms))
            {
                var coll = db.GetCollection<StreamTestsClass>("StreamTestsClass");
                // Do a find on a non-existant record
                var val = coll.FindById(new BsonValue(1));

                Assert.IsNull(val, "Empty database should not have record");
                val = new StreamTestsClass() { Id = 1, Value = "Hello" };
                coll.Insert(val);
            }

            using (var db = new LiteDatabase(ms))
            {
                var coll = db.GetCollection<StreamTestsClass>("StreamTestsClass");
                var val = coll.FindById(new BsonValue(1));

                Assert.IsNotNull(val, "Database should contain value from previous session");
            }
        }

        [TestMethod]
        public void FindOneClosesTransaction()
        {
            MemoryStream ms = new MemoryStream();

            using (var db = new LiteDatabase(ms))
            {
                var coll = db.GetCollection<StreamTestsClass>("StreamTestsClass");
                coll.Insert(new StreamTestsClass() { Id = 1, Value = "Record 1" });
                coll.Insert(new StreamTestsClass() { Id = 2, Value = "Record 1" });
                coll.Insert(new StreamTestsClass() { Id = 3, Value = "Record 1" });

                System.Diagnostics.Debug.WriteLine("Size:" + ms.Length);

                //                coll.FindOne(Query.)
                var rec = coll.FindOne(Query.EQ("Value", "Record 1"));

                coll.Insert(new StreamTestsClass() { Id = 4, Value = "Record 1" });

                System.Diagnostics.Debug.WriteLine("Size:" + ms.Length);
            }

            using (var db = new LiteDatabase(ms))
            {
                var coll = db.GetCollection<StreamTestsClass>("StreamTestsClass");
                var val = coll.FindAll();

                Assert.AreEqual(4, val.Count());
            }
        }

#if TEMP
        [ExpectedException(typeof(TransactionCancelledException))]
        [TestMethod]
        public void TransactionAbortCancelsOtherTransactions()
        {
            MemoryStream ms = new MemoryStream();

            using (var db = new LiteDatabase(ms))
            {
                var trans1 = db.BeginTrans();
                var trans2 = db.BeginTrans();

                trans1.Abort();
                trans2.Complete();
            }
        }
#endif

        [TestMethod]
        public void CreateIndexOnBsonDocument()
        {
            MemoryStream ms = new MemoryStream();

            using (var db = new LiteDatabase(ms))
            {
                var tc = new StreamTestsClass() { Id = 1, Value = "2" };
                var typedColl = db.GetCollection<StreamTestsClass>("StreamTestsClass");
                typedColl.Insert(tc);

                var coll = db.GetCollection("StreamTestsClass");
                var seasonCollResult = coll.FindOne(Query.EQ("Id", new BsonValue(1)));
            }
        }
    }
}

