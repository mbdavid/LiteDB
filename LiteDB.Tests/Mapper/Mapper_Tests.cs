using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Mapper
{
    public class Mapper_Tests
    {
        private BsonMapper _mapper = new BsonMapper();

        [Fact]
        public void ToDocument_ReturnsNull_WhenFail()
        {
            var array = new int[] { 1, 2, 3, 4, 5 };
            var doc1 = _mapper.ToDocument(array);
            doc1.Should<BsonDocument>().Be(null);

            var doc2 = _mapper.ToDocument(typeof(int[]), array);
            doc2.Should<BsonDocument>().Be(null);
        }
    }
}
