//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System.Collections.Generic;
//using System.Linq;

//namespace LiteDB.Tests.Database
//{
//    [TestClass]
//    public class IndexSortAndFilterTest
//    {
//        private LiteCollection<Item> _collection;
//        private TempFile _tempFile;
//        private LiteDatabase _database;

//        [TestInitialize]
//        public void Init()
//        {
//            _tempFile = new TempFile();
//            _database = new LiteDatabase(_tempFile.Filename);
//            _collection = _database.GetCollection<Item>("items");
//        }

//        [TestCleanup]
//        public void Cleanup()
//        {
//            _database.Dispose();
//            _tempFile.Dispose();
//        }

//        [TestMethod]
//        public void FilterAndSortAscending()
//        {
//            _collection.EnsureIndex(nameof(Item.Value));

//            PrepareData(_collection);
//            var result = FilterAndSortById(_collection, Query.Ascending);

//            Assert.AreEqual("B", result[0].Id);
//            Assert.AreEqual("C", result[1].Id);
//        }

//        [TestMethod]
//        public void FilterAndSortAscendingWithoutIndex()
//        {
//            PrepareData(_collection);
//            var result = FilterAndSortById(_collection, Query.Ascending);

//            Assert.AreEqual("B", result[0].Id);
//            Assert.AreEqual("C", result[1].Id);
//        }

//        [TestMethod]
//        public void FilterAndSortDescending()
//        {
//            _collection.EnsureIndex(nameof(Item.Value));

//            PrepareData(_collection);
//            var result = FilterAndSortById(_collection, Query.Descending);

//            Assert.AreEqual("C", result[0].Id);
//            Assert.AreEqual("B", result[1].Id);
//        }

//        private void PrepareData(LiteCollection<Item> collection)
//        {
//            collection.Upsert(new Item() { Id = "C", Value = "Value 1" });
//            collection.Upsert(new Item() { Id = "A", Value = "Value 2" });
//            collection.Upsert(new Item() { Id = "B", Value = "Value 1" });
//        }

//        private List<Item> FilterAndSortById(LiteCollection<Item> collection, int order)
//        {
//            var filterQuery = Query.EQ(nameof(Item.Value), "Value 1");
//            var sortQuery = Query.All(order);
//            var query = Query.And(sortQuery, filterQuery);

//            var result = collection.Find(query).ToList();
//            return result;
//        }

//        public class Item
//        {
//            public string Id { get; set; }

//            public string Value { get; set; }
//        }
//    }
//}