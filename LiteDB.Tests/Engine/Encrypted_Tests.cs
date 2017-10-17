using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Encrypted_Tests
    {
        [TestMethod]
        public void Encrypted_Database()
        {
            using (var encrypt = new TempFile())
            using (var plain = new TempFile())
            {
                // create a database with no password - plain data
                using (var db = new LiteEngine(plain.Filename))
                {
                    db.Insert("col", new BsonDocument { { "name", "Mauricio David" } });
                }

                // read datafile to find "Mauricio" string
                Assert.IsTrue(plain.ReadAsText().Contains("Mauricio David"));

                // create a database with password
                using (var db = new LiteEngine(encrypt.Filename, "abc123"))
                {
                    db.Insert("col", new BsonDocument { { "name", "Mauricio David" } });
                }

                // test if is possible find "Mauricio" string
                Assert.IsFalse(encrypt.ReadAsText().Contains("Mauricio David"));

                // try access using wrong password
                try
                {
                    using (var db = new LiteEngine(encrypt.Filename, "abc1234"))
                    {
                        Assert.Fail(); // can't work
                    }
                }
                catch (LiteException ex)
                {
                    Assert.IsTrue(ex.ErrorCode == 123); // wrong password
                }

                // open encrypted db and read document
                using (var db = new LiteEngine(encrypt.Filename, "abc123"))
                {
                    var doc = db.Find("col", Query.All()).First();

                    Assert.AreEqual("Mauricio David", doc["name"].AsString);

                    // let's remove password to work CheckIntegrety
                    db.Shrink(null, null);
                }
            }
        }
    }
}