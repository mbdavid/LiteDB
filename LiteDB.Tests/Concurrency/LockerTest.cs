using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class LockerTest
    {
        [TestMethod]
        public void Locker_Test()
        {
            using (var tmp = new TempFile())
            using (var db = new LiteEngine(tmp.Filename))
            {
                using (db.Locker.Shared())
                {
                    using (db.Locker.Shared())
                    {
                    }
                    using (db.Locker.Reserved())
                    {
                        using (db.Locker.Exclusive())
                        {
                        }
                    }
                }
            }
        }
    }
}