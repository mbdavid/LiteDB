using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public struct StructValue
    {
        public string Field { get; set; }
    }

    public class ContainerValue
    {
        public Guid Id { get; set; }
        public StructValue Struct { get; set; }
    }

    [TestClass]
    public class StructTest
    {
        [TestMethod]
        public void Struct_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteDatabase(file.Filename))
            {
                var col = db.GetCollection<ContainerValue>("col1");

                var id = Guid.NewGuid();

                col.Insert(new ContainerValue { Id = id, Struct = new StructValue { Field = "FieldValue" } });

                var item = col.FindById(id);

                Assert.AreEqual("FieldValue", item.Struct.Field);
            }
        }
    }
}