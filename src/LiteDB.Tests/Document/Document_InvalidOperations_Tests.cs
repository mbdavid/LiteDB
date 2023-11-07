namespace LiteDB.Tests.Document;

public class Document_InvalidOperations_Tests
{
    [Fact]
    public void BsonDocument_WritingToReadOnly_ShouldThrowException()
    {

        //Arrange
        var d = BsonDocument.Empty;

        //Act + Assert
        Assert.Throws<InvalidOperationException>(() => d["Name"] = "Rodolfo");                      //Try changing existing data
        Assert.Throws<InvalidOperationException>(() => d["Age"] = 26);                             //Try creating data
        Assert.Throws<InvalidOperationException>(() => d.Add("key", new BsonString("value")));    //Try creating data

    }



}
