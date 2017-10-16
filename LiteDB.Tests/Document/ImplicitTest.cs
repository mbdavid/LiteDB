using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
    [TestClass]
    public class ImplicitTest
    {
        [TestMethod]
        public void Implicit_Test()
        {
            int i = int.MaxValue;
            long l = long.MaxValue;
            uint ui = uint.MaxValue;
            ulong ul = ulong.MaxValue;

            BsonValue bi = i;
            BsonValue bl = l;
            BsonValue bui = ui;
            BsonValue bul = ul;

            Assert.IsTrue(bi.IsInt32);
            Assert.IsTrue(bl.IsInt64);
            Assert.IsTrue(bui.IsDouble);
            Assert.IsTrue(bul.IsDouble);

            Assert.AreEqual(i, bi.AsInt32);
            Assert.AreEqual(l, bl.AsInt64);
            Assert.AreEqual(ui, bui.AsDouble);
            Assert.AreEqual(ul, bul.AsDouble);
        }
    }
}