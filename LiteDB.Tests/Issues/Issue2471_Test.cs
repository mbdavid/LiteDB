using FluentAssertions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace LiteDB.Tests.Issues;

public class Issue2471_Test
{
    [Fact]
    public void TestFragmentDB_FindByIDException()
    {
        using var db = new LiteDatabase(":memory:");
        var collection = db.GetCollection<object>("fragtest");

        var fragment = new object { };
        var id = collection.Insert(fragment);

        id.Should().BeGreaterThan(0);

        var frag2 = collection.FindById(id);
        frag2.Should().NotBeNull();

        Action act = () => db.Checkpoint();

        act.Should().NotThrow();
    }

    [Fact]
    public void MultipleReadCleansUpTransaction()
    {
        using var database = new LiteDatabase(":memory:");

        var collection = database.GetCollection("test");
        collection.Insert(new BsonDocument { ["_id"] = 1 });

        for (int i = 0; i < 500; i++)
        {
            collection.FindById(1);
        }
    }
}