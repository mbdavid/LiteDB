using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Document
{
    public class Implicit_Tests
    {
        [Fact]
        public void BsonValue_Implicit_Convert()
        {
            int i = int.MaxValue;
            long l = long.MaxValue;
            ulong u = ulong.MaxValue;

            BsonValue bi = i;
            BsonValue bl = l;
            BsonValue bu = u;

            bi.IsInt32.Should().BeTrue();
            bl.IsInt64.Should().BeTrue();
            bu.IsDouble.Should().BeTrue();

            bi.AsInt32.Should().Be(i);
            bl.AsInt64.Should().Be(l);
            bu.AsDouble.Should().Be(u);
        }
    }
}