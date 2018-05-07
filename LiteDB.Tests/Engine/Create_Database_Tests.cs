using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Create_Database_Tests
    {
        [TestMethod]
        public void Create_Database_With_Initial_Size()
        {
            var initial = 40 * 1024; // initial size: 40kb
            var minimal = 4096 * 5; // 1 header + 1 lock + 1 collection + 1 data + 1 index = 5 pages minimal

            using (var file = new TempFile())
            using (var db = new LiteDatabase(file.Conn("initial size=40kb")))
            {
                // just ensure open datafile
                var uv = db.Engine.UserVersion;

                // test if file has 40kb
                Assert.AreEqual(initial, file.Size);

                // simple insert to test if datafile still with 40kb
                db.Engine.Run("db.col1.insert {a:1}"); // use 3 pages to this

                Assert.AreEqual(initial, file.Size);

                // ok, now shrink and test if file are minimal size
                db.Shrink();

                Assert.AreEqual(minimal, file.Size);
            }
        }

        [TestMethod]
        public void Create_Database_With_Initial_Size_Encrypted()
        {
            var initial = 40 * 1024; // initial size: 40kb
            var minimal = 4096 * 5; // 1 header + 1 lock + 1 collection + 1 data + 1 index = 5 pages minimal

            using (var file = new TempFile(checkIntegrity: false))
            using (var db = new LiteDatabase(file.Conn("initial size=40kb; password=123")))
            {
                // just ensure open datafile
                var uv = db.Engine.UserVersion;

                // test if file has 40kb
                Assert.AreEqual(initial, file.Size);

                // simple insert to test if datafile still with 40kb
                db.Engine.Run("db.col1.insert {a:1}"); // use 3 pages to this

                Assert.AreEqual(initial, file.Size);

                // ok, now shrink and test if file are minimal size
                db.Shrink();

                Assert.AreEqual(minimal, file.Size);
            }
        }
    }
}