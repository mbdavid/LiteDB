
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.Tests
{
    public struct StructValue
    {
        public string Field { get; set; }
    }

    public class Container
    {
        public Guid Id { get; set; }

        public StructValue Struct { get; set; }
    }

    [TestClass]
    public class StructTest : TestBase
    {
        [TestMethod]
        public void Struct_Test()
        {
            var m = new MemoryStream();

            using (var db = new LiteDatabase(m))
            {
                var col = db.GetCollection<Container>("col1");

                var id = Guid.NewGuid();
                col.Insert(new Container() { Id = id, Struct = new StructValue() { Field = "FieldValue" } });
                var item = col.FindById(id);
                Assert.AreEqual("FieldValue", item.Struct.Field);
            }
        }
    }
}
