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
    // [TestClass]
    public class BigFileTest
    {
        // [TestMethod]
        public void BigFile_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                // create more than 4gb file
                while(file.Size < 4L * 1024 * 1024 * 1024)
                {
                    db.Insert("col", GetDocs(5000));
                } 

                // now lets read all docs
                foreach(var d in db.Find("col", Query.All()))
                {
                    // just read to check if there is any exception
                }

            }
        }

        private IEnumerable<BsonDocument> GetDocs(int count)
        {
            var rnd = new Random();

            for (var i = 0; i < count; i++)
            {
                yield return new BsonDocument
                {
                    { "_id", ObjectId.NewObjectId() },
                    { "html", TempFile.LoremIpsum(15, 50, 4, 10, 3) }
                };
            }
        }
    }
}