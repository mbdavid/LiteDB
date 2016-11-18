using System;
using System.IO;
using LiteDB.Tests.NetCore;

namespace LiteDB.Tests
{
    public class BsonTest : TestBase
    {
        private BsonDocument CreateDoc()
        {
            // create same object, but using BsonDocument
            var doc = new BsonDocument();
            doc["_id"] = 123;
            doc["FirstString"] = "BEGIN this string \" has \" \t and this \f \n\r END";
            doc["CustomerId"] = Guid.NewGuid();
            doc["Date"] = DateTime.Now;
            doc["MyNull"] = null;
            doc["EmptyObj"] = new BsonDocument();
            doc["EmptyString"] = "";
            doc["maxDate"] = DateTime.MaxValue;
            doc["minDate"] = DateTime.MinValue;

            doc.Set("Customer.Address.Street", "Av. Caçapava, Nº 122");

            doc["Items"] = new BsonArray();

            doc["Items"].AsArray.Add(new BsonDocument());
            doc["Items"].AsArray[0].AsDocument["Qtd"] = 3;
            doc["Items"].AsArray[0].AsDocument["Description"] = "Big beer package";
            doc["Items"].AsArray[0].AsDocument["Unit"] = (double)10 / (double)3;

            doc["Items"].AsArray.Add("string-one");
            doc["Items"].AsArray.Add(null);
            doc["Items"].AsArray.Add(true);
            doc["Items"].AsArray.Add(DateTime.Now);

            return doc;
        }

        public void Bson_Test()
        {
            var test_name = "Bson_Test";
            var o = CreateDoc();

            var bson = BsonSerializer.Serialize(o);
            var json = JsonSerializer.Serialize(o);

            var doc = BsonSerializer.Deserialize(bson);

            Helper.AssertIsTrue(test_name, 0, 123 == doc["_id"].AsInt32);
            Helper.AssertIsTrue(test_name, 1, o["_id"].AsInt64 == doc["_id"].AsInt64);

            Helper.AssertIsTrue(test_name, 2, "Av. Caçapava, Nº 122" == doc.Get("Customer.Address.Street").AsString);

            Helper.AssertIsTrue(test_name, 3, o["FirstString"].AsString == doc["FirstString"].AsString);
            Helper.AssertIsTrue(test_name, 4, o["Date"].AsDateTime.ToString() == doc["Date"].AsDateTime.ToString());
            Helper.AssertIsTrue(test_name, 5, o["CustomerId"].AsGuid == doc["CustomerId"].AsGuid);
            Helper.AssertIsTrue(test_name, 6, o["MyNull"].RawValue == doc["MyNull"].RawValue);
            Helper.AssertIsTrue(test_name, 7, o["EmptyString"].AsString == doc["EmptyString"].AsString);

            Helper.AssertIsTrue(test_name, 8, DateTime.MaxValue == doc["maxDate"].AsDateTime);
            Helper.AssertIsTrue(test_name, 9, DateTime.MinValue == doc["minDate"].AsDateTime);

            Helper.AssertIsTrue(test_name, 10, o["Items"].AsArray.Count == doc["Items"].AsArray.Count);
            Helper.AssertIsTrue(test_name, 11, o["Items"].AsArray[0].AsDocument["Unit"].AsDouble == doc["Items"].AsArray[0].AsDocument["Unit"].AsDouble);
            Helper.AssertIsTrue(test_name, 12, o["Items"].AsArray[4].AsDateTime.ToString() == doc["Items"].AsArray[4].AsDateTime.ToString());
        }
    }
}