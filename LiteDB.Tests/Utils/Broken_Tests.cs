using Xunit;

namespace LiteDB.Tests
{
    public class Broken_Tests
    {
        [Fact(Skip = "Throws exception (obviously)")]
        public void Throw_Exception()
        {
            throw new System.Exception("Exception from forced-throwing test");
        }
    }
}