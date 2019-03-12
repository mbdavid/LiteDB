using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;
using System.Threading;
using System.Linq.Expressions;
using System.Diagnostics;

namespace LiteDB.Tests.Mapper
{
    [TestClass]
    public class GenericMap_Tests
    {
        public class User<T, K>
        {
            public T Id { get; set; }
            public K Name { get; set; }
        }

        private BsonMapper _mapper = new BsonMapper();

        [TestMethod]
        public void Generic_Map()
        {
            var guid = Guid.NewGuid();
            var today = DateTime.Today;

            var u0 = new User<int, string> { Id = 1, Name = "John" };
            var u1 = new User<double, Guid> { Id = 99.9, Name = guid };
            var u2 = new User<DateTime, string> { Id = today, Name = "Carlos" };
            var u3 = new User<Dictionary<string, object>, string>
            {
                Id = new Dictionary<string, object> { ["f"] = "user1", ["n"] = 4 },
                Name = "Complex User"
            };

            var d0 = _mapper.ToDocument(u0.GetType(), u0);
            var d1 = _mapper.ToDocument(u1.GetType(), u1);
            var d2 = _mapper.ToDocument(u2.GetType(), u2);
            var d3 = _mapper.ToDocument(u3.GetType(), u3);

            Assert.AreEqual(1, d0["_id"].AsInt32);
            Assert.AreEqual("John", d0["Name"].AsString);

            Assert.AreEqual(99.9, d1["_id"].AsDouble);
            Assert.AreEqual(guid, d1["Name"].AsGuid);

            Assert.AreEqual(today, d2["_id"].AsDateTime);
            Assert.AreEqual("Carlos", d2["Name"].AsString);

            Assert.AreEqual("user1", d3["_id"].AsDocument["f"].AsString);
            Assert.AreEqual(4, d3["_id"].AsDocument["n"].AsInt32);
            Assert.AreEqual("Complex User", d3["Name"].AsString);

        }
    }
}