using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    /// <summary>
    /// Testing issue #501
    /// </summary>
    public interface IPartner
    {
        string PartnerId
        {
            get;
        }

        string HostId
        {
            get;
        }
    }

    // must be public
    public class Partner : IPartner
    {
        // must have public non-parameter ctor
        public Partner()
        {
        }

        public Partner(string partnerId, string hostId)
        {
            this.PartnerId = partnerId;
            this.HostId = hostId;
        }

        // must have get/set
        public string PartnerId
        {
            get; set;
        }

        public string HostId
        {
            get; set;
        }
    }

    [TestClass]
    public class InterfaceBaseTest
    {
        [TestMethod]
        public void InterfaceBase_Test()
        {
            var m = new BsonMapper();
            var p = new Partner("one", "host1");

            var doc = m.ToDocument(p);

            Assert.AreEqual("one", doc["_id"].AsString);
            Assert.AreEqual("host1", doc["HostId"].AsString);

            var no = m.ToObject<Partner>(doc);

            Assert.AreEqual("one", no.PartnerId);
            Assert.AreEqual("host1", no.HostId);
        }
    }
}