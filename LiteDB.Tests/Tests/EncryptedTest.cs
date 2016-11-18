using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB.Shell;

namespace LiteDB.Tests
{
    [TestClass]
    public class EncryptedTest : TestBase
    {
        [TestMethod]
        public void Encrypted_Test()
        {
            using (var encrypt = new TempFile("password=abc"))
            using (var plain = new TempFile())
            {
                // create a database with no password - plain data
                using (var db = new LiteDatabase(plain.ConnectionString))
                {
                    db.Run("db.col1.insert {name:\"Mauricio David\"}");
                }

                // read datafile to find "Mauricio" string
                Assert.IsTrue(TestPlatform.FileReadAllText(plain.Filename).Contains("Mauricio David"));

                // create a database with password
                using (var db = new LiteDatabase(encrypt.ConnectionString))
                {
                    db.Run("db.col1.insert {name:\"Mauricio David\"}");
                }

                // test if is possible find "Mauricio" string
                Assert.IsFalse(TestPlatform.FileReadAllText(encrypt.Filename).Contains("Mauricio David"));

                // try access using wrong password
                using (var db = new LiteDatabase(encrypt.ConnectionString + "X"))
                {
                    try
                    {
                        db.Run("show collections");

                        Assert.Fail(); // can't work
                    }
                    catch (LiteException ex)
                    {
                        Assert.IsTrue(ex.ErrorCode == 123); // wrong password
                    }
                }
            }
        }
    }
}