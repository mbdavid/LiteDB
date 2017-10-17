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
    public class Engine_Tests
    {
        [TestMethod]
        public void Engine_Insert_Documents()
        {
            using (var file = new TempFile())
            {
                using (var db = new LiteEngine(file.Filename))
                {
                    db.Insert("col", new BsonDocument { { "_id", 1 } , { "name", "John" } });
                    db.Insert("col", new BsonDocument { { "_id", 2 }, { "name", "Doe" } });
                }

                using (var db = new LiteEngine(file.Filename))
                {
                    var john = db.Find("col", Query.EQ("_id", 1)).FirstOrDefault();
                    var doe = db.Find("col", Query.EQ("_id", 2)).FirstOrDefault();

                    Assert.AreEqual("John", john["name"].AsString);
                    Assert.AreEqual("Doe", doe["name"].AsString);
                }
            }
        }

        [TestMethod]
        public void Engine_Upsert_Documents()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                var doc1 = new BsonDocument { { "_id", 1 }, { "name", "John" } };

                var u1 = db.Upsert("col", doc1); // true (insert)

                doc1["name"] = "changed";

                var u2 = db.Upsert("col", doc1); // false (update)

                Assert.AreEqual(true, u1);
                Assert.AreEqual(false, u2);

                // get data from db
                var r = db.Find("col", Query.EQ("_id", 1)).Single();

                // test changed value
                Assert.AreEqual(doc1["name"].AsString, r["name"].AsString);
            }
        }

        [TestMethod]
        public void Engine_Delete_Documents()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                var doc1 = new BsonDocument { { "_id", 1 }, { "name", "John" } };
                var doc2 = new BsonDocument { { "_id", 2 }, { "name", "Doe" } };

                db.Insert("col", doc1);
                db.Insert("col", doc2);

                db.Delete("col", Query.GTE("_id", 1));

                db.Insert("col", doc1);
            }
        }

        public void Engine_QueryUpdate_Documents()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.EnsureIndex("col", "name");

                // insert 4 documents
                db.Insert("col", new BsonDocument { { "_id", 1 } });
                db.Insert("col", new BsonDocument { { "_id", 2 } });
                db.Insert("col", new BsonDocument { { "_id", 3 } });
                db.Insert("col", new BsonDocument { { "_id", 4 } });

                // query all documents and update name
                foreach(var d in db.Find("col", Query.All()))
                {
                    d["name"] = "john";
                    db.Update("col", d);
                }

                // this simple test if same thread open a read mode and then open write lock mode
                Assert.AreEqual(4, db.Count("col", Query.EQ("name", "john")));
            }
        }
    }
}