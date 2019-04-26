using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LiteDB.Tests.CustomMapper.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.CustomMapper
{
    [TestClass]
    public class Custom_Mapper_Tests
    {
        private BsonMapper _mapper;

        [TestInitialize]
        public void Initialize()
        {
            _mapper = new CollectionMapperClass();
        }

        private ItemCollection CreateCollection()
        {
            var items = new ItemCollection()
            {
                MyItemCollectionName = "MyCollection"
            };
            items.Add(new Item()
            {
                MyItemName = "MyItem"
            });
            return items;
        }
        [TestMethod]
        public BsonDocument ShouldSerializeCollectionClass()
        {
            var items = CreateCollection();

            var document = _mapper.ToDocument(items);
            Assert.AreEqual("MyCollection", (string)document["MyItemCollectionName"]);

            var array = (BsonArray)document["_items"];
            Assert.IsNotNull(array);
            var recoveritem = (BsonDocument)array[0];
            Assert.AreEqual("MyItem", (string)recoveritem["MyItemName"]);
            return document;
        }

        [TestMethod]
        public void ShouldDeserializeCollectionClass()
        {

            var items = CreateCollection();
            var document = _mapper.ToDocument(items);


            var lst = (ItemCollection)_mapper.Deserialize(typeof(ItemCollection), document);

            Assert.AreEqual("MyCollection", lst.MyItemCollectionName);
            Assert.AreEqual(lst.Count, 1);
            Assert.IsInstanceOfType(lst[0], typeof(Item));
            Assert.AreEqual(lst[0].MyItemName,"MyItem");
        }

        [TestMethod]
        public void ShouldInsertIntoDatabaseAndRecover()
        {
            var items = CreateCollection();
            using (var repository = new LiteRepository(new MemoryStream(), _mapper))
            {
               var result= repository.Upsert<ItemCollection>(items);
                Assert.IsTrue(result);
                Assert.AreNotEqual(Guid.Empty,items.Id);
                var lst = repository.SingleById<ItemCollection>(items.Id);
                Assert.AreEqual("MyCollection", lst.MyItemCollectionName);
                Assert.AreEqual(lst.Count, 1);
                Assert.IsInstanceOfType(lst[0], typeof(Item));
                Assert.AreEqual(lst[0].MyItemName, "MyItem");
            }
        }
    }
}
