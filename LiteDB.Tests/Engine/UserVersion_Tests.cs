using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class UserVersion_Tests
    {
        [TestMethod]
        public void UserVersion_Get_Set()
        {
            using (var file = new TempFile())
            {
                using (var db = new LiteEngine(file.Filename))
                {
                    Assert.AreEqual(0, db.UserVersion);
                    db.UserVersion = 5;
                }

                using (var db = new LiteEngine(file.Filename))
                {
                    Assert.AreEqual(5, db.UserVersion);
                }
            }
        }
    }
}