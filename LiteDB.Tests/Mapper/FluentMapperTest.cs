using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
#if !NET35
using System.Threading.Tasks;
#endif

namespace LiteDB.Tests
{
    public class FluentClass
    {
        public int CurrentKey { get; set; }
        public Func<string> GetPath { get; set; }
        public string PropName { get; set; }
#if !NET35
        // testing if DbAsync will be added
        private Task<string> DbAsync { get { return new Task<string>(() => "task"); } }
#endif
    }

    [TestClass]
    public class FluentMapperTest
    {
        [TestMethod]
        public void FluentMapper_Test()
        {
            var o = new FluentClass
            {
                CurrentKey = 1,
                GetPath = () => "",
                PropName = "name"                
            };

            var m = new BsonMapper();

            m.Entity<FluentClass>()
                .Id(x => x.CurrentKey)
                .Ignore(x => x.GetPath)
                .Field(x => x.PropName, "prop_name");

            var d = m.ToDocument(o);

            Assert.AreEqual(1, d["_id"].AsInt32);
            Assert.IsFalse(d.Keys.Contains("GetPath"));
            Assert.AreEqual("name", d["prop_name"].AsString);
        }
    }
}