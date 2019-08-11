using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Mapper
{
    public class CustomMapping_Tests
    {
        public class User
        {
            public int Id { get; }
            public string Name { get; }

            public User(int id, string name)
            {
                this.Id = id;
                this.Name = name;
            }
        }

        public class Domain
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class MultiCtor
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string DefinedOnlyInInt32 { get; set; }

            public MultiCtor()
            {
            }

            [BsonCtor]
            public MultiCtor(int id)
            {
                this.Id = id;
                this.DefinedOnlyInInt32 = "changed";
            }

            public MultiCtor(int id, string name)
            {
                this.Id = id;
                this.Name = name;
            }
        }

        private BsonMapper _mapper = new BsonMapper();

        [Fact]
        public void Custom_Ctor()
        {
            var doc = new BsonDocument { ["_id"] = 10, ["name"] = "John" };

            var user = _mapper.ToObject<User>(doc);

            user.Id.Should().Be(10);
            user.Name.Should().Be("John");
        }

        [Fact]
        public void ParameterLess_Ctor()
        {
            var doc = new BsonDocument { ["_id"] = 25, ["name"] = "numeria.com.br" };

            var domain = _mapper.ToObject<Domain>(doc);

            domain.Id.Should().Be(25);
            domain.Name.Should().Be("numeria.com.br");
        }

        [Fact]
        public void BsonCtor_Attribute()
        {
            var doc = new BsonDocument { ["_id"] = 25, ["name"] = "value-name" };

            var obj = _mapper.ToObject<MultiCtor>(doc);

            obj.Id.Should().Be(25);
            obj.Name.Should().Be("value-name");
            obj.DefinedOnlyInInt32.Should().Be("changed");
        }
    }
}