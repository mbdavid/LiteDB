using LiteDB.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class TempStream_Tests
    {
        [TestMethod]
        public void TempStream_Create_Memory_Disk()
        {
            var filename = "";

            using (var t = new TempStream(4000))
            {
                Assert.IsTrue(t.InMemory);
                Assert.IsFalse(t.InDisk);

                // writing some stufs
                var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                t.Position = 10;
                t.Write(buffer, 0, buffer.Length);

                // read from memory
                var temp = new byte[10];
                t.Position = 10;
                t.Read(temp, 0, temp.Length);

                // compare is both are same
                Assert.IsTrue(buffer.SequenceEqual(temp));

                // now extend to file creation
                t.Position = 5000;

                // test if is in disk
                Assert.IsTrue(t.InDisk);
                Assert.IsFalse(t.InMemory);

                // read again, now from disk
                var temp2 = new byte[10];

                t.Position = 10;
                t.Read(temp2, 0, temp2.Length);

                // compare is both are same
                Assert.IsTrue(buffer.SequenceEqual(temp2));

                // get filename to test if was deleted
                filename = t.Filename;
            }

            // check if file as deleted
            Assert.IsFalse(File.Exists(filename));

        }
    }
}