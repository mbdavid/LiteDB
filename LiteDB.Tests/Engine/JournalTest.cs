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
    public class JournalTest
    {
        [TestMethod]
        public void Journal_Test()
        {
            using (var file = new TempFile())
            {
                // initialize datafile with 5 pages
                using (var db = new LiteEngine(file.Filename))
                {
                    db.Insert("a", new BsonDocument { ["a"] = 1 });
                }

                // update 4 pages
                using (var db = new LiteEngine(file.Filename))
                {
                    db.Insert("a", new BsonDocument { ["b"] = 2 });
                }


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