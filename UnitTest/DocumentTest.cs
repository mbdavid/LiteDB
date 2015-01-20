using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    [TestClass]
    public class DocumentTest
    {
        [TestMethod]
        public void Document_Create()
        {
            var now = DateTime.Now;
            var cid = Guid.NewGuid();

            // create a typed object
            var orderObject = new Order
            {
                OrderKey = 123,
                CustomerId = cid,
                Date = now,
                Items = new List<OrderItem>() {
                    new OrderItem { Qtd = 3, Description = "Package", Unit = 99m } 
                }
            };

            // create same object, but using BsonDocument
            var orderDoc = new BsonDocument();
            orderDoc.Id = 123;
            orderDoc["CustomerId"] = cid;
            orderDoc["Date"] = now;
            orderDoc["Items"] = new BsonArray();
            var i = new BsonObject();
            i["Qtd"] = 3;
            i["Description"] = "Package";
            i["Unit"] = 99m;
            orderDoc["Items"].AsArray.Add(i);

            // serialize both and get indexKey for each one
            var bytesObject = BsonSerializer.Serialize(orderObject);
            var keyObject = new IndexKey(BsonSerializer.GetIdValue(orderObject));

            var bytesDoc = BsonSerializer.Serialize(orderDoc);
            var keyDoc = new IndexKey(BsonSerializer.GetIdValue(orderDoc));

            // lets revert objects (create a object from Document and create a Document from a object)
            var revertObject = BsonSerializer.Deserialize<Order>(keyDoc, bytesDoc);
            var revertDoc = BsonSerializer.Deserialize<BsonDocument>(keyObject, bytesObject);

            // lets compare properties

            Assert.AreEqual(revertObject.OrderKey, revertDoc.Id);
            Assert.AreEqual(revertObject.CustomerId, revertDoc["CustomerId"].AsGuid);
            Assert.AreEqual(revertObject.Date, revertDoc["Date"].AsDateTime);
            Assert.AreEqual(revertObject.Items[0].Unit, revertDoc["Items"][0]["Unit"].AsDecimal);

            // get some property
            Assert.AreEqual(now, BsonSerializer.GetFieldValue(revertObject, "Date"));
            Assert.AreEqual(now, BsonSerializer.GetFieldValue(revertDoc, "Date"));

            Assert.AreEqual(cid, BsonSerializer.GetFieldValue(revertObject, "CustomerId"));
            Assert.AreEqual(cid, BsonSerializer.GetFieldValue(revertDoc, "CustomerId"));

            Assert.AreEqual(null, BsonSerializer.GetFieldValue(revertObject, "Date2"));
            Assert.AreEqual(null, BsonSerializer.GetFieldValue(revertDoc, "Date2"));

        }
    }
}
