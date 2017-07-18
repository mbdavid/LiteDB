using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Tests.Database
{
    [TestClass]
    public class LinqContainsTests
    {
        private TempFile _tempFile;
        private LiteDatabase _database;
        private LiteCollection<ItemWithEnumerable> _collection;

        public class ItemWithEnumerable
        {
            public int[] Array { get; set; }

            public IEnumerable<int> Enumerable { get; set; }
        }

        [TestInitialize]
        public void Init()
        {
            _tempFile = new TempFile();
            _database = new LiteDatabase(_tempFile.Filename);
            _collection = _database.GetCollection<ItemWithEnumerable>("items");

            var item = new ItemWithEnumerable()
            {
                Array = new int[] { 1 },
                Enumerable = new List<int>() { 2 },
            };

            _collection.Insert(item);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _database.Dispose();
            _tempFile.Dispose();
        }

        [TestMethod]
        public void ArrayContains()
        {
            var result = _collection.Find(i => i.Array.Contains(1)).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void ListContains()
        {
            var result = _collection.Find(i => i.Enumerable.Contains(2)).ToList();
            Assert.AreEqual(1, result.Count);
        }
    }
}