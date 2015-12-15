using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LiteDB.Tests
{
    public class TestPocoClass
    {
        public String Key { get; set; }
        public Int32 Data { get; set; }
    }

    [TestClass]
    public class MyTest
    {
        private volatile static LiteDatabase db;
        private volatile static LiteCollection<TestPocoClass> col;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            db = new LiteDatabase(new MemoryStream());//@"d:\test.ldb");
            col = db.GetCollection<TestPocoClass>("col1");
            col.EnsureIndex(o => o.Key);
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            db.Dispose();
            //File.Delete(@"d:\test.ldb");
        }

        [TestMethod]
        public void TestMethod1()
        {
            Assert.AreEqual(col.Count(), 0);
            var tf = Task.Factory.StartNew(FillData);
            Task.WaitAll(tf);
            var Data = col.FindOne(o => o.Key == "Test1");
            Assert.AreNotEqual(Data, null);
            Assert.AreEqual(Data.Data, 1);
            tf = Task.Factory.StartNew(UpdateData);
            Task.WaitAll(tf);
            Data = col.FindOne(o => o.Key == "Test1");
            Assert.AreNotEqual(Data, null);
            Assert.AreEqual(Data.Data, 77);
            db.DropCollection("col1");
            Assert.AreEqual(db.CollectionExists("col1"), false);
        }

        private void FillData()
        {
            TestPocoClass c = new TestPocoClass()
            {
                Key = "Test1",
                Data = 1
            };
            col.Insert(c);
        }

        private void UpdateData()
        {
            var Data = col.FindOne(o => o.Key == "Test1");
            Assert.AreNotEqual(Data, null);
            Data.Data = 77;
            col.Update(Data);
        }
    }
}