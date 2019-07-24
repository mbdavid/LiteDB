using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Mapper
{
    public class GenericMap_Tests
    {
        public class User<T, K>
        {
            public T Id { get; set; }
            public K Name { get; set; }
        }

        private BsonMapper _mapper = new BsonMapper();

        [Fact]
        public void Generic_Map()
        {
            var guid = Guid.NewGuid();
            var today = DateTime.Today;

            var u0 = new User<int, string> {Id = 1, Name = "John"};
            var u1 = new User<double, Guid> {Id = 99.9, Name = guid};
            var u2 = new User<DateTime, string> {Id = today, Name = "Carlos"};
            var u3 = new User<Dictionary<string, object>, string>
            {
                Id = new Dictionary<string, object> {["f"] = "user1", ["n"] = 4},
                Name = "Complex User"
            };

            var d0 = _mapper.ToDocument(u0.GetType(), u0);
            var d1 = _mapper.ToDocument(u1.GetType(), u1);
            var d2 = _mapper.ToDocument(u2.GetType(), u2);
            var d3 = _mapper.ToDocument(u3.GetType(), u3);

            d0["_id"].AsInt32.Should().Be(1);
            d0["Name"].AsString.Should().Be("John");

            d1["_id"].AsDouble.Should().Be(99.9);
            d1["Name"].AsGuid.Should().Be(guid);

            d2["_id"].AsDateTime.Should().Be(today);
            d2["Name"].AsString.Should().Be("Carlos");

            d3["_id"]["f"].AsString.Should().Be("user1");
            d3["_id"]["n"].AsInt32.Should().Be(4);
            d3["Name"].AsString.Should().Be("Complex User");
        }
    }
}