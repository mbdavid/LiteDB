using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Mapper
{
    public class Dictionary_Tests
    {
        public class Dict
        {
            public IDictionary<DateTime, string> DateDict { get; set; } = new Dictionary<DateTime, string>();
        }

        private BsonMapper _mapper = new BsonMapper();

        [Fact]
        public void Dictionary_Map()
        {
            var obj = new Dict();

            obj.DateDict[DateTime.Now] = "now!";

            var doc = _mapper.ToDocument(obj);

            var newobj = _mapper.ToObject<Dict>(doc);

            newobj.DateDict.Keys.First().Should().Be(obj.DateDict.Keys.First());
        }
    }
}