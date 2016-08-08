using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using LiteDB.Shell;

namespace LiteDB.Tests
{
    [TestClass]
    public class DiskFileTest : TestBase
    {
        [TestMethod]
        public void DiskFile_Test()
        {
            using (var tmp = new TempFile())
            {
                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    db.Run("db.col1.insert {_id:10}");
                    db.Run("db.col1.insert {_id:2}");
                    db.Run("db.col1.insert {_id:3}");

                    var col1 = db.GetCollection("col1");

                    var d = col1.FindAll().First();

                    db.Run("db.col1.insert {_id:4}");
                }
            }
        }
    }
}