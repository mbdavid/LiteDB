using FluentAssertions;
using LiteDB.Engine;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace LiteDB.Tests.Engine
{
    public class Collation_Tests
    {
        [Fact]
        public void Culture_Ordinal_Sort()
        {
            var collation = new Collation(1046, CompareOptions.IgnoreCase);

            var s = new EngineSettings
            {
                DataStream = new MemoryStream(),
                Collation = collation
            };

            var names = new string[] { "Ze", "Ana", "Ána", "Ánã", "Ana Paula", "ana lucia" };

            var sortByLinq = names.OrderBy(x => x, collation).ToArray();
            var findByLinq = names.Where(x => collation.Compare(x, "ANA") == 0).ToArray();

            using(var e = new LiteEngine(s))
            {
                e.Insert("col1", names.Select(x => new BsonDocument { ["name"] = x }), BsonAutoId.Int32);

                // sort by merge sort
                var sortByOrderByName = e.Query("col1", new Query { OrderBy = "name" })
                    .ToEnumerable()
                    .Select(x => x["name"].AsString)
                    .ToArray();

                var query = new Query();
                query.Where.Add("name = 'ANA'");

                // find by expression
                var findByExpr = e.Query("col1", query)
                    .ToEnumerable()
                    .Select(x => x["name"].AsString)
                    .ToArray();

                sortByOrderByName.Should().BeEquivalentTo(sortByLinq);
                findByExpr.Should().BeEquivalentTo(findByLinq);

                // index test
                e.EnsureIndex("col1", "idx_name", "name", false);

                // sort by index
                var sortByIndexName = e.Query("col1", new Query { OrderBy = "name" })
                    .ToEnumerable()
                    .Select(x => x["name"].AsString)
                    .ToArray();

                // find by index
                var findByIndex = e.Query("col1", query)
                    .ToEnumerable()
                    .Select(x => x["name"].AsString)
                    .ToArray();

                sortByIndexName.Should().BeEquivalentTo(sortByLinq);
                findByIndex.Should().BeEquivalentTo(findByLinq);
            }

        }
    }
}