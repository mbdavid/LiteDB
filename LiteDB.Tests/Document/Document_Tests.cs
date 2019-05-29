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
        //** [TestMethod]
        //** public void Document_Implicit_Convert()
        //** {
        //**     var obj = new Dictionary<string, object>()
        //**     {
        //**         { "int", 123 },
        //**         { "arr", new object[] { 3.0, 2, 1, "zero", false } },
        //**         { "doc", new Dictionary<string, object>()
        //**             {
        //**                 { "a", "a" },
        //**                 { "b", new int[] { 0 } },
        //**             }
        //**         }
        //**     };
        //** 
        //**     var doc = new BsonValue(obj);
        //** 
        //**     var json = JsonSerializer.Serialize(doc);
        //** 
        //**     Assert.AreEqual("{\"int\":123,\"arr\":[3.0,2,1,\"zero\",false],\"doc\":{\"a\":\"a\",\"b\":[0]}}", json);
        //** }

        [TestMethod]
        public void Document_Copies_Properties_To_KeyValue_Array()
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
        }

        [TestMethod]
        public void Value_Index_From_BsonValue()
        {
            var arr = JsonSerializer.Deserialize("[0, 1, 2, 3]");
            var doc = JsonSerializer.Deserialize("{a:1,b:2,c:3}");

            Assert.AreEqual(0, arr[0].RawValue);
            Assert.AreEqual(3, arr[3].RawValue);

            Assert.AreEqual(1, doc["a"].RawValue);
            Assert.AreEqual(3, doc["c"].RawValue);

            arr[1] = 111;
            doc["b"] = 222;

            Assert.AreEqual(111, arr[1].RawValue);
            Assert.AreEqual(222, doc["b"].RawValue);
        }
    }
}