#if NET35
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests
{
    public class AutoIdClass
    {
        public int Id { get; set; }
        public int From { get; set; }
    }

    [TestClass]
    public class AutoIdConcurrencyTest
    {
        [TestMethod]
        public void AutoIdProcess_Test()
        {
            // Test 2 instances including in some collection
            // and test if no duplicate key will be throwed
            using (var file = new TempFile())
            {
                using (var dbA = new LiteDatabase(file.Filename))
                using (var dbB = new LiteDatabase(file.Filename))
                {
                    var colA = dbA.GetCollection<AutoIdClass>("col1");
                    var colB = dbB.GetCollection<AutoIdClass>("col1");

                    // insert 900 x From=1
                    var ta = Task.Factory.StartNew(() =>
                    {
                        for (var i = 0; i < 900; i++)
                        {
                            colA.Insert(new AutoIdClass { From = 1 });
                        }
                    });

                    // insert 1100 x From=2
                    var tb = Task.Factory.StartNew(() =>
                    {
                        for (var i = 0; i < 1100; i++)
                        {
                            colB.Insert(new AutoIdClass { From = 2 });
                        }
                    });

                    Task.WaitAll(ta, tb);

                    Assert.AreEqual(2000, colA.Count());
                    Assert.AreEqual(2000, colB.Count());

                    Assert.AreEqual(2000, colA.Max().AsInt32);
                    Assert.AreEqual(2000, colB.Max().AsInt32);

                    Assert.AreEqual(900, colA.Count(x => x.From == 1));
                    Assert.AreEqual(1100, colA.Count(x => x.From == 2));

                    Assert.AreEqual(900, colB.Count(x => x.From == 1));
                    Assert.AreEqual(1100, colB.Count(x => x.From == 2));

                }
            }
        }

        [TestMethod]
        public void AutoIdThread_Test()
        {
            // Test 2 instances including in some collection
            // and test if no duplicate key will be throwed
            using (var file = new TempFile())
            {
                using (var db = new LiteDatabase(file.Filename))
                {
                    var col = db.GetCollection<AutoIdClass>("col1");

                    // insert 900 x From=1
                    var ta = Task.Factory.StartNew(() =>
                    {
                        for (var i = 0; i < 900; i++)
                        {
                            col.Insert(new AutoIdClass { From = 1 });
                        }
                    });

                    // insert 1100 x From=2
                    var tb = Task.Factory.StartNew(() =>
                    {
                        for (var i = 0; i < 1100; i++)
                        {
                            col.Insert(new AutoIdClass { From = 2 });
                        }
                    });

                    Task.WaitAll(ta, tb);

                    Assert.AreEqual(2000, col.Count());
                    Assert.AreEqual(2000, col.Max().AsInt32);

                    Assert.AreEqual(900, col.Count(x => x.From == 1));
                    Assert.AreEqual(1100, col.Count(x => x.From == 2));
                }
            }
        }
    }
}
#endif