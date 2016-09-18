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
    [TestClass]
    public class CacheTest
    {
        [TestMethod]
        public void CacheCheckpoint_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            { 
                var log = new StringBuilder();
                db.Log.Level = Logger.CACHE;
                db.Log.Logging += (s) => log.AppendLine(s);

                // insert basic 200.000 documents
                db.Insert("col", GetDocs(200000));

                Assert.IsTrue(log.ToString().Contains("checkpoint"));
            }
        }

        private IEnumerable<BsonDocument> GetDocs(int count)
        {
            for(var i = 0; i < count; i++)
            {
                yield return new BsonDocument
                {
                    { "_id", i },
                    { "name", Guid.NewGuid().ToString() },
                    { "lorem", TempFile.LoremIpsum(3, 5, 2, 3, 3) }
                };
            }
        }
    }
}