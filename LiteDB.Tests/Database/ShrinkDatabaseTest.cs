using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests
{
    public class LargeDoc
    {
        public ObjectId Id { get; set; }
        public Guid Guid { get; set; }
        public string Lorem { get; set; }
    }

    [TestClass]
    public class ShrinkDatabaseTest
    {
        [TestMethod]
        public void ShrinkDatabaseTest_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteDatabase(file.Filename))
            {
                var col = db.GetCollection<LargeDoc>("col");

                col.Insert(GetDocs(1, 10000));

                db.DropCollection("col");

                // full disk usage
                var size = file.Size;

                var r = db.Shrink();

                // only header page
                Assert.AreEqual(8192, size - r);
            }
        }

        private IEnumerable<LargeDoc> GetDocs(int initial, int count)
        {
            for (var i = initial; i < initial + count; i++)
            {
                yield return new LargeDoc
                {
                    Guid = Guid.NewGuid(),
                    Lorem = TempFile.LoremIpsum(10, 15, 2, 3, 3)
                };
            }
        }
    }
}