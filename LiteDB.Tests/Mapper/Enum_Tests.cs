using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Mapper
{
    public class Enum_Tests
    {
        public enum CustomerType
        {
            Potential,
            New,
            Loyal
        }
        public class Customer
        {
            public int Id { get; set; }
            public CustomerType Type { get; set; }
            public CustomerType? NullableType { get; set; }
        }

        [Fact]
        public void Enum_Convert_Into_Document()
        {
            var mapper = new BsonMapper();

            var c = new Customer { Id = 1, Type = CustomerType.Loyal };

            var doc = mapper.ToDocument(c);

            doc["Type"].AsString.Should().Be("Loyal");
            doc["NullableType"].IsNull.Should().BeTrue();

            var fromDoc = mapper.ToObject<Customer>(doc);

            fromDoc.Type.Should().Be(CustomerType.Loyal);
            fromDoc.NullableType.Should().BeNull();

        }

        [Fact]
        public void Enum_Convert_Into_Linq_Query()
        {
            var mapper = new BsonMapper();

            var c = new Customer { Id = 1, Type = CustomerType.Loyal };

            //mapper.enu

            var doc = mapper.ToDocument(c);

            var expr1 = mapper.GetExpression<Customer, bool>(x => x.Type == CustomerType.Loyal);
            var expr2 = mapper.GetExpression<Customer, bool>(x => x.NullableType == CustomerType.Loyal);

            ;
        }

    }
}