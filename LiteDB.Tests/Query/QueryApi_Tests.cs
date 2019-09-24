using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace LiteDB.Tests.QueryTest
{
    public class QueryApi_Tests : Person_Tests
    {
        [Fact(Skip = "must fix multi expressions parameters")]
        public void Query_And()
        {
            var r0 = local.Where(x => x.Age == 22 && x.Active == true).ToArray();

            var r1 = collection.Find(Query.And(Query.EQ("Age", 22), Query.EQ("Active", true))).ToArray();

            AssertEx.ArrayEqual(r0, r1, true);

        }
    }
}