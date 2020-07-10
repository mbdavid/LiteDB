using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Mapper
{
    public class CustomMappingCtor_Tests
    {
        public class UserWithCustomId
        {
            public int Key { get; }
            public string Name { get; }

            public UserWithCustomId(int key, string name)
            {
                this.Key = key;
                this.Name = name;
            }
        }

        [Fact]
        public void Custom_Ctor_With_Custom_Id()
        {
            var mapper = new BsonMapper();

            mapper.Entity<UserWithCustomId>()
                .Id(u => u.Key, false);

            var doc = new BsonDocument { ["_id"] = 10, ["name"] = "John" };

            var user = mapper.ToObject<UserWithCustomId>(doc);

            user.Key.Should().Be(10); //     Expected user.Key to be 10, but found 0.
            user.Name.Should().Be("John");
        }
    }
}