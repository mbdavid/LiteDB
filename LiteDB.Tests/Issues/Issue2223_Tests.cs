using Xunit;

namespace LiteDB.Tests.Issues
{
    public class Issue2223_Tests
    {
        class BaseClass
        {
            public int A { get; set; }
        }

        class DerivedClass : BaseClass
        {
            public int B { get; set; }
        }

        [Fact]
        public void Serialize_covariant_type_uses_type_parameter()
        {
            var derived = new DerivedClass()
            {
                A = 1,
                B = 2,
            };

            var mapper = new BsonMapper();

            var docA = mapper.Serialize<BaseClass>(derived);
            var docB = mapper.Deserialize<DerivedClass>(docA);

            Assert.True(docB.B == 0);
        }
    }
}
