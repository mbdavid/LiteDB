using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Document
{
    [TestClass]
    public class Decimal_Tests
    {
        [TestMethod]
        public void BsonValueDecimal()
        {
            var d0 = 0m;
            var d1 = 1m;
            var dmin = new BsonValue(decimal.MinValue);
            var dmax = new BsonValue(decimal.MaxValue);

            Assert.AreEqual("{\"$numberDecimal\":\"0\"}", JsonSerializer.Serialize(d0));
            Assert.AreEqual("{\"$numberDecimal\":\"1\"}", JsonSerializer.Serialize(d1));
            Assert.AreEqual("{\"$numberDecimal\":\"-79228162514264337593543950335\"}", JsonSerializer.Serialize(dmin));
            Assert.AreEqual("{\"$numberDecimal\":\"79228162514264337593543950335\"}", JsonSerializer.Serialize(dmax));

            var b0 = BsonSerializer.Serialize(new BsonDocument { { "A", d0 } });
            var b1 = BsonSerializer.Serialize(new BsonDocument { { "A", d1 } });
            var bmin = BsonSerializer.Serialize(new BsonDocument { { "A", dmin } });
            var bmax = BsonSerializer.Serialize(new BsonDocument { { "A", dmax } });

            var x0 = BsonSerializer.Deserialize(b0);
            var x1 = BsonSerializer.Deserialize(b1);
            var xmin = BsonSerializer.Deserialize(bmin);
            var xmax = BsonSerializer.Deserialize(bmax);

            Assert.AreEqual(d0, x0["A"].AsDecimal);
            Assert.AreEqual(d1, x1["A"].AsDecimal);
            Assert.AreEqual(dmin, xmin["A"].AsDecimal);
            Assert.AreEqual(dmax, xmax["A"].AsDecimal);

        }
    }
}