using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
    public class Broken_Tests
    {
        [TestMethod]
        public void Throw_Exception()
        {
            throw new System.Exception("Exception from forced-throwing test");
        }
        
    }
}