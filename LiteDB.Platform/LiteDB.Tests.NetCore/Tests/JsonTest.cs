using System;
using System.IO;
using LiteDB.Tests.NetCore;

namespace LiteDB.Tests
{
    public class JsonTest : TestBase
    {
        private BsonDocument CreateDoc()
        {
            // create same object, but using BsonDocument
            var doc = new BsonDocument();
            doc["_id"] = 123;
            doc["Special"] = "Màçã ámö-î";
            doc["FirstString"] = "BEGIN this string \" has \" \t and this \f \n\r END";
            doc["CustomerId"] = Guid.NewGuid();
            doc["Date"] = new DateTime(2015, 1, 1);
            doc["MyNull"] = null;
            doc["Items"] = new BsonArray();
            doc["MyObj"] = new BsonDocument();
            doc["EmptyString"] = "";
            var obj = new BsonDocument();
            obj["Qtd"] = 3;
            obj["Description"] = "Big beer package";
            obj["Unit"] = 1299.995;
            doc["Items"].AsArray.Add(obj);
            doc["Items"].AsArray.Add("string-one");
            doc["Items"].AsArray.Add(null);
            doc["Items"].AsArray.Add(true);
            doc["Items"].AsArray.Add(DateTime.Now);

            doc.Set("MyObj.IsFirstId", true);

            return doc;
        }

        public void Json_Test()
        {
            var test_name = "Json_Test";
            var o = CreateDoc();

            var json = JsonSerializer.Serialize(o, true);

            var doc = JsonSerializer.Deserialize(json).AsDocument;

            Helper.AssertIsTrue(test_name, 0, o["Special"].AsString == doc["Special"].AsString);
            Helper.AssertIsTrue(test_name, 1, o["Date"].AsDateTime == doc["Date"].AsDateTime);
            Helper.AssertIsTrue(test_name, 2, o["CustomerId"].AsGuid == doc["CustomerId"].AsGuid);
            Helper.AssertIsTrue(test_name, 3, o["Items"].AsArray.Count == doc["Items"].AsArray.Count);
            Helper.AssertIsTrue(test_name, 4, 123 == doc["_id"].AsInt32);
            Helper.AssertIsTrue(test_name, 5, o["_id"].AsInt64 == doc["_id"].AsInt64);
        }
    }
}