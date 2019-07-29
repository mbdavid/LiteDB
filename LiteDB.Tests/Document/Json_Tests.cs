using System;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Document
{
    public class Json_Tests
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


            return doc;
        }

        [Fact]
        public void Json_To_Document()
        {
            var o = CreateDoc();

            var json = JsonSerializer.Serialize(o);

            var doc = JsonSerializer.Deserialize(json).AsDocument;

            doc["Special"].AsString.Should().Be(o["Special"].AsString);
            doc["Date"].AsDateTime.Should().Be(o["Date"].AsDateTime);
            doc["CustomerId"].AsGuid.Should().Be(o["CustomerId"].AsGuid);
            doc["Items"].AsArray.Count.Should().Be(o["Items"].AsArray.Count);
            doc["_id"].AsInt32.Should().Be(123);
            doc["_id"].AsInt64.Should().Be(o["_id"].AsInt64);
        }
    }
}