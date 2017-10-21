using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LiteDB.Tests.Mapper
{
    #region Model

    public class CustomType
    {
        public Regex Re1 { get; set; }
        public Regex Re2 { get; set; }

        public Uri Url { get; set; }
        public TimeSpan Ts { get; set; }
    }

    #endregion

    [TestClass]
    public class Custom_Types_Tests
    {
        [TestMethod]
        public void Custom_Types()
        {
            var mapper = new BsonMapper();

            var o = new CustomType
            {
                Re1 = new Regex("^a+"),
                Re2 = new Regex("^a*", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace),
                Url = new Uri("http://www.litedb.org"),
                Ts = TimeSpan.FromSeconds(10)
            };

            var doc = mapper.ToDocument(o);
            var no = mapper.ToObject<CustomType>(doc);

            Assert.AreEqual("^a+", doc["Re1"].AsString);
            Assert.AreEqual("^a*", doc["Re2"].AsDocument["p"].AsString);

            Assert.AreEqual(o.Re1.ToString(), no.Re1.ToString());
            Assert.AreEqual(o.Re2.ToString(), no.Re2.ToString());
            Assert.AreEqual(o.Re2.Options, no.Re2.Options);
            Assert.AreEqual(o.Url, no.Url);
            Assert.AreEqual(o.Ts, no.Ts);
        }
    }
}