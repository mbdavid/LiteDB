using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LiteDB.Tests
{
    public class TestPocoClass
    {
        [BsonId]
        public string Key { get; set; }
        public int Info { get; set; }
    }

    [TestClass]
    public class Task_Test
    {
        private static LiteDatabase db;
        private static LiteCollection<TestPocoClass> col;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            db = new LiteDatabase(new MemoryStream());
            col = db.GetCollection<TestPocoClass>("col1");
            col.EnsureIndex(o => o.Key);
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            db.Dispose();
        }

        [TestMethod]
        public void FindLocker_Test()
        {
            Assert.AreEqual(col.Count(), 0);

            // insert data
            Task.Factory.StartNew(InsertData).Wait();

            // test inserted data :: Info = 1
            var data = col.FindOne(o => o.Key == "Test1");
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Info);

            // update data :: Info = 77
            Task.Factory.StartNew(UpdateData).Wait();

            // find updated data
            data = col.FindOne(o => o.Key == "Test1");
            Assert.IsNotNull(data);
            Assert.AreEqual(77, data.Info);

            // drop collection
            db.DropCollection("col1");
            Assert.AreEqual(db.CollectionExists("col1"), false);
        }

        private void InsertData()
        {
            var data = new TestPocoClass()
            {
                Key = "Test1",
                Info = 1
            };
            col.Insert(data);
        }

        private void UpdateData()
        {
            var data = col.FindOne(o => o.Key == "Test1");
            Assert.IsNotNull(data);
            data.Info = 77;
            col.Update(data);
        }
    }
}