using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.QueryTest
{
    public class Select_Tests : Person_Tests
    {
        [Fact]
        public void Query_Select_Key_Only()
        {
            collection.EnsureIndex(x => x.Address.City);

            // must orderBy mem data because index will be sorted
            var r0 = local
                .OrderBy(x => x.Address.City)
                .Select(x => x.Address.City)
                .ToArray();

            // this query will not deserialize document, using only index key
            var r1 = collection.Query()
                .OrderBy(x => x.Address.City)
                .Select(x => x.Address.City)
                .ToArray();

            r0.Should().Equal(r1);
        }

        [Fact]
        public void Query_Select_New_Document()
        {
            var r0 = local
                .Select(x => new {city = x.Address.City.ToUpper(), phone0 = x.Phones[0], address = new Address {Street = x.Name}})
                .ToArray();

            var r1 = collection.Query()
                .Select(x => new {city = x.Address.City.ToUpper(), phone0 = x.Phones[0], address = new Address {Street = x.Name}})
                .ToArray();

            foreach (var r in r0.Zip(r1, (l, r) => new {left = l, right = r}))
            {
                r.right.city.Should().Be(r.left.city);
                r.right.phone0.Should().Be(r.left.phone0);
                r.right.address.Street.Should().Be(r.left.address.Street);
            }
        }

        [Fact]
        public void Query_Or_With_Null()
        {
            var r = collection.Find(Query.Or(
                Query.GTE("Date", new DateTime(2001, 1, 1)),
                Query.EQ("Date", null)
            ));
        }

        [Fact]
        public void Query_Find_All_Predicate()
        {
            var r = collection.Find(x => true).ToArray();

            r.Should().HaveCount(1000);
        }
    }
}