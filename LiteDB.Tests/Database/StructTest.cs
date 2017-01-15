using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public struct StructValue
    {
        public string Property { get; set; }
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
            var mapper = new BsonMapper();
            mapper.IncludeFields = true;

            var obj = new ContainerValue
            {
                Id = Guid.NewGuid(),
                Struct = new StructValue
                {
                    Property = "PropertyValue"
                }
            };

            var doc = mapper.ToDocument(obj);
            var nobj = mapper.ToObject<ContainerValue>(doc);

            Assert.AreEqual(obj.Id, nobj.Id);
            Assert.AreEqual(obj.Struct.Property, nobj.Struct.Property);

        }
    }
}