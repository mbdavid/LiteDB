using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;

namespace LiteDB.Tests.Mapper
{
    #region Model

    public class AttrCustomer
    {
        [BsonId]
        public int MyPK { get; set; }
        [BsonField("name")]
        public string NameCustomer { get; set; }
        [BsonIgnore]
        public bool Ignore { get; set; }
        [BsonRef("address")]
        public AttrAddress Address { get; set; }
        [BsonRef("address")]
        public List<AttrAddress> Addresses { get; set; }
    }

    public class AttrAddress
    {
        [BsonId]
        public int AddressPK { get; set; }
        public string Street { get; set; }
    }

    #endregion

    [TestClass]
    public class Attribute_Mapper_Tests
    {
        [TestMethod]
        public void Attribute_Mapper()
        {
            var mapper = new BsonMapper();

            var c0 = new AttrCustomer
            {
                MyPK = 1,
                NameCustomer = "J",
                Address = new AttrAddress { AddressPK = 5, Street = "R" },
                Ignore = true,
                Addresses = new List<AttrAddress>()
                {
                    new AttrAddress { AddressPK = 3 },
                    new AttrAddress { AddressPK = 4 }
                }
            };

            var j0 = JsonSerializer.Serialize(mapper.ToDocument(c0));

            var c1 = mapper.ToObject<AttrCustomer>(JsonSerializer.Deserialize(j0).AsDocument);

            Assert.AreEqual(c0.MyPK, c1.MyPK);
            Assert.AreEqual(c0.NameCustomer, c1.NameCustomer);
            Assert.AreEqual(false, c1.Ignore);
            Assert.AreEqual(c0.Address.AddressPK, c1.Address.AddressPK);
            Assert.AreEqual(c0.Addresses[0].AddressPK, c1.Addresses[0].AddressPK);
            Assert.AreEqual(c0.Addresses[1].AddressPK, c1.Addresses[1].AddressPK);

        }
    }
}