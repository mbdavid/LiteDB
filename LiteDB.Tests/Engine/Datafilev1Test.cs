#if !PCL
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using LiteDB.Shell;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class Datafilev1Test : TestBase
    {
        [TestMethod]
        public void Datafilev1_Test()
        {
            // try open a old datafile version to check if message are correct
            var v1 =  Path.GetFullPath(@"..\..\v1.db");

            try
            {
                using (var db = new LiteDatabase(v1))
                {
                    db.GetCollectionNames().ToList();
                    Assert.Fail("Version 1 must not work");
                }
            }
            catch(LiteException ex)
            {
                if(ex.ErrorCode == LiteException.INVALID_DATABASE || ex.ErrorCode == LiteException.INVALID_DATABASE_VERSION)
                {
                    // ok
                }
                else
                {
                    Assert.Fail("Invalid exception for old v1 data format");
                }
            }
        }
    }
}
#endif