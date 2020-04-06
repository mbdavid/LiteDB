using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using LiteDB.Engine;
using Xunit;

namespace LiteDB.Tests.Database
{
    public class Create_Database_Tests
    {
        [Fact]
        public void Create_Database_With_Initial_Size()
        {
            var initial = 10 * 8192; // initial size: 80kb
            var minimal = 8192 * 4; // 1 header + 1 collection + 1 data + 1 index = 4 pages minimal

            using (var file = new TempFile())
            {
                using (var db = new LiteDatabase("filename=" + file.Filename + ";initial size=" + initial))
                {
                    var col = db.GetCollection("col");

                    // just ensure open datafile
                    col.FindAll().ToArray();

                    // test if file has 40kb
                    file.Size.Should().Be(initial);

                    // simple insert to test if datafile
                    col.Insert(new BsonDocument { ["_id"] = 1 }); // use 4 pages to this (1 data, 1 index, 1 collection + header)

                    // after checkpoint must keep same initial size
                    db.Checkpoint();

                    file.Size.Should().Be(initial);

                    // ok, now shrink and test if file are minimal size
                    db.Rebuild();

                    file.Size.Should().Be(minimal);
                }
            }
        }
    }
}