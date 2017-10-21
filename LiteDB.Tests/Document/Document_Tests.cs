using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Document
{
    [TestClass]
    public class Document_Test
    {
        [TestMethod]
        public void Document_ImplicitConvert_Test()
        {
            var obj = new Dictionary<string, object>()
            {
                { "int", 123 },
                { "arr", new object[] { 3.0, 2, 1, "zero", false } },
                { "doc", new Dictionary<string, object>()
                    {
                        { "a", "a" },
                        { "b", new int[] { 0 } },
                    }
                }
            };

            var doc = new BsonValue(obj);

            var json = JsonSerializer.Serialize(doc, false, true);

            Assert.AreEqual("{\"int\":123,\"arr\":[3.0,2,1,\"zero\",false],\"doc\":{\"a\":\"a\",\"b\":[0]}}", json);
        }

        [TestMethod]
        public void Document_copies_properties_to_KeyValue_array()
        {
            // ARRANGE
            // create a Bson document with all possible value types

            var document = new BsonDocument();
            document.Add("string", new BsonValue("string"));
            document.Add("bool", new BsonValue(true));
            document.Add("objectId", new BsonValue(ObjectId.NewObjectId()));
            document.Add("DateTime", new BsonValue(DateTime.Now));
            document.Add("decimal", new BsonValue((decimal)1));
            document.Add("double", new BsonValue((double)1.0));
            document.Add("guid", new BsonValue(Guid.NewGuid()));
            document.Add("int", new BsonValue((int)1));
            document.Add("long", new BsonValue((long)1));
            document.Add("bytes", new BsonValue(new byte[] { (byte)1 }));
            document.Add("bsonDocument", new BsonDocument());

            // ACT
            // copy all properties to destination array

            var result = new KeyValuePair<string, BsonValue>[document.Count()];
            document.CopyTo(result, 0);

            // ASSERT
            // all BsonValue instances have been added to the array by reference

            //TODO: implement get from another way
            // Assert.IsTrue(result.All(kv => object.ReferenceEquals(document.Get(kv.Key), kv.Value)));
        }
    }
}