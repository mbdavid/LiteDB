//using System;
//using System.Collections.Generic;
//using System.Linq;
//using FluentAssertions;
//using Xunit;

//namespace LiteDB.Tests.Database
//{
//    public class IndexSortAndFilterTest : IDisposable
//    {
//        #region Model

//        public class Item
//        {
//            public string Id { get; set; }
//            public string Value { get; set; }
//        }

//        #endregion

//        private LiteCollection<Item> _collection;
//        private TempFile _tempFile;
//        private LiteDatabase _database;

//        public IndexSortAndFilterTest()
//        {
//            _tempFile = new TempFile();
//            _database = new LiteDatabase(_tempFile.Filename);
//            _collection = _database.GetCollection<Item>("items");
//        }

//        public void Dispose()
//        {
//            _database.Dispose();
//            _tempFile.Dispose();
//        }

//        [Fact]
//        public void FilterAndSortAscending()
//        {
//            _collection.EnsureIndex(nameof(Item.Value));

//            PrepareData(_collection);
//            var result = FilterAndSortById(_collection, Query.Ascending);

//            result[0].Id.Should().Be("B");
//            result[1].Id.Should().Be("C");
//        }

//        [Fact]
//        public void FilterAndSortAscendingWithoutIndex()
//        {
//            PrepareData(_collection);
//            var result = FilterAndSortById(_collection, Query.Ascending);

//            result[0].Id.Should().Be("B");
//            result[1].Id.Should().Be("C");
//        }

//        [Fact]
//        public void FilterAndSortDescending()
//        {
//            _collection.EnsureIndex(nameof(Item.Value));

//            PrepareData(_collection);
//            var result = FilterAndSortById(_collection, Query.Descending);

//            result[0].Id.Should().Be("C");
//            result[1].Id.Should().Be("B");
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

//            var result = collection.Find(filterQuery).ToList();
//            return result;
//        }
//    }
//}