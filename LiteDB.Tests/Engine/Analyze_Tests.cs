using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Engine
{
    public class Analyze_Tests
    {
        [Fact]
        public void Analyze_Collection_Count()
        {
            var zip = DataGen.Zip().Take(100).ToArray();

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var col = db.GetCollection<Zip>();

                // init zip collection with 100 document
                col.Insert(zip);

                col.EnsureIndex(x => x.City);
                col.EnsureIndex(x => x.Loc);

                var indexes = db.GetCollection("$indexes").FindAll()
                    .ToDictionary(x => x["name"].AsString, x => x, StringComparer.OrdinalIgnoreCase);

                // testing for just-created indexes (always be zero)
                indexes["_id"]["keyCount"].AsInt32.Should().Be(0);
                indexes["_id"]["uniqueKeyCount"].AsInt32.Should().Be(0);
                indexes["city"]["uniqueKeyCount"].AsInt32.Should().Be(0);
                indexes["loc"]["uniqueKeyCount"].AsInt32.Should().Be(0);

                // but indexes created after data exists will count
                indexes["city"]["keyCount"].AsInt32.Should().Be(100);
                indexes["loc"]["keyCount"].AsInt32.Should().Be(200);

                db.Analyze("zip");

                indexes = db.GetCollection("$indexes").FindAll()
                    .ToDictionary(x => x["name"].AsString, x => x, StringComparer.OrdinalIgnoreCase);

                // count unique values
                var uniqueCity = new HashSet<string>(zip.Select(x => x.City));
                var uniqueLoc = new HashSet<double>(zip.Select(x => x.Loc[0]).Union(zip.Select(x => x.Loc[1])));

                indexes["_id"]["keyCount"].AsInt32.Should().Be(zip.Length);
                indexes["_id"]["uniqueKeyCount"].AsInt32.Should().Be(zip.Length);
                indexes["city"]["keyCount"].AsInt32.Should().Be(zip.Length);
                indexes["city"]["uniqueKeyCount"].AsInt32.Should().Be(uniqueCity.Count);

                indexes["loc"]["keyCount"].AsInt32.Should().Be(zip.Length * 2);
                indexes["loc"]["uniqueKeyCount"].AsInt32.Should().Be(uniqueLoc.Count);
            }
        }
    }
}