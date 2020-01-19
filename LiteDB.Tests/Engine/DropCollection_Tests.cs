using System.Linq;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Engine
{
    public class DropCollection_Tests
    {
        [Fact]
        public void DropCollection()
        {
            using (var file = new TempFile())
            using (var db = new LiteDatabase(file.Filename))
            {
                db.GetCollectionNames().Should().NotContain("col");

                var col = db.GetCollection("col");

                col.Insert(new BsonDocument {["a"] = 1});

                db.GetCollectionNames().Should().Contain("col");

                db.DropCollection("col");

                db.GetCollectionNames().Should().NotContain("col");
            }
        }
    }
}