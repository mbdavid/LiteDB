using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiteDB.Tests.NetCore.Tests
{
    public class EncryptedTest : TestBase
    {
        public void Encrypted_Test()
        {
            string test_name = "Encrypted_Test";

            using (var encrypt = new TempFile("password=abc"))
            using (var plain = new TempFile())
            {
                // create a database with no password - plain data
                using (var db = new LiteDatabase(plain.ConnectionString))
                {
                    db.Run("db.col1.insert {name:\"Mauricio David\"}");
                }

                // read datafile to find "Mauricio" string
                Helper.AssertIsTrue(test_name, 0, TestPlatform.FileReadAllText(plain.Filename).Contains("Mauricio David"));

                // create a database with password
                using (var db = new LiteDatabase(encrypt.ConnectionString))
                {
                    db.Run("db.col1.insert {name:\"Mauricio David\"}");
                }

                // test if is possible find "Mauricio" string
                Helper.AssertIsTrue(test_name, 1, !TestPlatform.FileReadAllText(encrypt.Filename).Contains("Mauricio David"));

                // try access using wrong password
                using (var db = new LiteDatabase(encrypt.ConnectionString + "X"))
                {
                    try
                    {
                        db.Run("show collections");

                        Helper.AssertIsTrue(test_name, 2, false); // can't work
                    }
                    catch (LiteException ex)
                    {
                        Helper.AssertIsTrue(test_name, 2, (ex.ErrorCode == 123)); // wrong password
                    }
                }
            }
        }
    }
}
