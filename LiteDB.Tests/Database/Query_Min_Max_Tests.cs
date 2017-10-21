using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Database
{
    #region Model

    public class EntityMinMax
    {
        public int Id { get; set; }
        public byte ByteValue { get; set; }
        public int IntValue { get; set; }
        public uint UintValue { get; set; }
        public long LongValue { get; set; }
    }

    #endregion

    [TestClass]
    public class Query_Min_Max_Tests
    {
        [TestMethod]
        public void Query_Min_Max()
        {
            using (var f = new TempFile())
            using (var db = new LiteDatabase(f.Filename))
            {
                var c = db.GetCollection<EntityMinMax>("col");

                c.Insert(new EntityMinMax { });
                c.Insert(new EntityMinMax
                {
                    ByteValue = 200,
                    IntValue = 443500,
                    LongValue = 443500,
                    UintValue = 443500
                });

                c.EnsureIndex(x => x.ByteValue);
                c.EnsureIndex(x => x.IntValue);
                c.EnsureIndex(x => x.LongValue);
                c.EnsureIndex(x => x.UintValue);

                Assert.AreEqual(200, c.Max(x => x.ByteValue).AsInt32);
                Assert.AreEqual(443500, c.Max(x => x.IntValue).AsInt32);
                Assert.AreEqual(443500, c.Max(x => x.LongValue).AsInt64);
                Assert.AreEqual(443500, c.Max(x => x.UintValue).AsInt32);

            }
        }
    }
}