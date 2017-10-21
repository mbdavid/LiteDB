using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Mapper
{
    #region Model

    public struct StructValue
    {
        public string Property { get; set; }
    }

    public class ContainerValue
    {
        public Guid Id { get; set; }
        public StructValue Struct { get; set; }
    }

    #endregion

    [TestClass]
    public class Struct_Tests
    {
        [TestMethod]
        public void Struct_Mapper()
        {
            var mapper = new BsonMapper
            {
                IncludeFields = true
            };

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