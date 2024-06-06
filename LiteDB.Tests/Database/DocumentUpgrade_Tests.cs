using FluentAssertions;

using LiteDB.Engine;

using System.IO;

using Xunit;

namespace LiteDB.Tests.Database;

public class DocumentUpgrade_Tests
{
    [Fact]
    public void DocumentUpgrade_Test()
    {
        var ms = new MemoryStream();
        using (var db = new LiteDatabase(ms))
        {
            var col = db.GetCollection("col");

            col.Insert(new BsonDocument { ["version"] = 1, ["_id"] = 1, ["name"] = "John" });
        }

        ms.Position = 0;

        using (var db = new LiteDatabase(ms))
        {
            var col = db.GetCollection("col");

            col.Count().Should().Be(1);

            var doc = col.FindById(1);

            doc["version"].AsInt32.Should().Be(1);
            doc["name"].AsString.Should().Be("John");
            doc["age"].AsInt32.Should().Be(0);
        }

        ms.Position = 0;

        var engine = new LiteEngine(new EngineSettings
        {
            DataStream = ms,
            ReadTransform = ReadTransform
        });

        using (var db = new LiteDatabase(engine))
        {
            var col = db.GetCollection("col");

            col.Count().Should().Be(1);

            var doc = col.FindById(1);

            doc["version"].AsInt32.Should().Be(2);
            doc["name"].AsString.Should().Be("John");
            doc["age"].AsInt32.Should().Be(30);
        }
    }

    private BsonValue ReadTransform(string arg1, BsonValue val)
    {
        if (!(val is BsonDocument bdoc))
        {
            return val;
        }

        if (bdoc.TryGetValue("version", out var version) && version.AsInt32 == 1)
        {
            bdoc["version"] = 2;
            bdoc["age"] = 30;
        }

        return val;
    }
}