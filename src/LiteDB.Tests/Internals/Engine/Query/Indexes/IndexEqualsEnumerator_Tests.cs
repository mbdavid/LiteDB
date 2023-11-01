//using NSubstitute;
//using System.Threading.Tasks;

//namespace LiteDB.Tests.Internals.Engine;

//public class IndexEqualsEnumerator_Tests
//{
//    private readonly IndexDocument _doc = Substitute.For<IndexDocument>();
//    private readonly IIndexService _indexService = new MockIndexService();

//    [Fact]
//    public void MoveNextAsync()
//    {
//        #region Arrange
//        var indexDocument = new IndexDocument()
//        {
//            Slot = 0,
//            Name = "_id",
//            Expression = "$._id",
//            Unique = true,
//            HeadIndexNodeID = new RowID(0, 1),
//            TailIndexNodeID = new RowID(10, 1)

//        };
//        var _sut = new IndexEqualsEnumerator(1, indexDocument, Collation.Default);

//        var pipeContext = new PipeContext(null, _indexService, new BsonDocument());

//        #endregion


//        #region Act
//        var value = _sut.MoveNextAsync(pipeContext);
//        #endregion


//        #region Asserts
//        value.Should().Be(new RowID(0, 0));
//        #endregion
//    }
//}