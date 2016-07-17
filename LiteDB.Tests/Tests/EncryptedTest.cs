using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB.Shell;

namespace LiteDB.Tests
{
    [TestClass]
    public class EncryptedTest : TestBase
    {
        [TestMethod]
        public void Encrypted_Order()
        {
            var encrypt = DB.RandomFile();
            var plain = DB.RandomFile();

            var cs_enc = "password=abc;filename=" + encrypt;
            var cs_enc_wrong = "password=abcd;filename=" + encrypt;

            // create a database with no password - plain data
            using (var db = new LiteDatabase(plain))
            {
                db.Run("db.col1.insert {name:\"Mauricio David\"}");
            }

            // read datafile to find "Mauricio" string
            Assert.IsTrue(TestPlatform.FileReadAllText(plain).Contains("Mauricio David"));

            // create a database with password
            using (var db = new LiteDatabase(cs_enc))
            {
                db.Run("db.col1.insert {name:\"Mauricio David\"}");
            }

            // test if is possible find "Mauricio" string
            Assert.IsFalse(TestPlatform.FileReadAllText(encrypt).Contains("Mauricio David"));

            // try access using wrong password
            using (var db = new LiteDatabase(cs_enc_wrong))
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

            TestPlatform.DeleteFile(plain);
            TestPlatform.DeleteFile(encrypt);
        }
    }
}