using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Shrink_Tests
    {
        [TestMethod, TestCategory("Engine")]
        public void Shrink_Database()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.InsertBulk("col", GetDocs(1, 10000));

                db.DropCollection("col");

                // full disk usage
                var size = file.Size;

                var r = db.Shrink();

                // only header page + reserved lock page
                Assert.AreEqual(8192, size - r);
            }
        }

        private IEnumerable<BsonDocument> GetDocs(int initial, int count)
        {
            for (var i = initial; i < initial + count; i++)
            {
                yield return new BsonDocument
                {
                    ["Guid"] = Guid.NewGuid(),
                    ["Lorem"] = TempFile.LoremIpsum(10, 15, 2, 3, 3)
                };
            }
        }
    }
}