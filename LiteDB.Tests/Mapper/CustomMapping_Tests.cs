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

        public interface IInterface
        {
            string CustomId { get; set; }
        }

        public class ConcreteClass : IInterface
        {
            public string CustomId { get; set; }
        }

        [Fact]
        public void Custom_Id_In_Interface()
        {
            var mapper = new BsonMapper();

            mapper.Entity<IInterface>().Id(x => x.CustomId, false);

            var obj = new ConcreteClass { CustomId = "myid" };
            var doc = mapper.Serialize(obj) as BsonDocument;
            doc["_id"].Should().NotBeNull();
            doc["_id"].Should().Be("myid");
            doc.Keys.ExpectCount(1);
        }
    }
}