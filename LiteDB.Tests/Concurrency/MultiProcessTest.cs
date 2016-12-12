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
    [TestClass]
    public class MultiProcessTest
    {
        [TestMethod]
        public void MultiProcess_Insert_Test()
        {
            using (var file = new TempFile())
            {
                using (var dbA = new LiteEngine(file.Filename))
                using (var dbB = new LiteEngine(file.Filename))
                {
                    dbA.EnsureIndex("col", "process", false);

                    // insert 1000 x instance=1
                    var ta = Task.Factory.StartNew(() =>
                    {
                        for (var i = 0; i < 1000; i++)
                        {
                            dbA.Insert("col", new BsonDocument { { "process", 1 } });
                        }
                    });

                    // insert 700 x instance=2
                    var tb = Task.Factory.StartNew(() =>
                    {
                        for (var i = 0; i < 700; i++)
                        {
                            dbB.Insert("col", new BsonDocument { { "process", 2 } });
                        }
                    });

                    Task.WaitAll(ta, tb);

                    Assert.AreEqual(1000, dbA.Count("col", Query.EQ("process", 1)));
                    Assert.AreEqual(700, dbA.Count("col", Query.EQ("process", 2)));

                    Assert.AreEqual(1000, dbB.Count("col", Query.EQ("process", 1)));
                    Assert.AreEqual(700, dbB.Count("col", Query.EQ("process", 2)));

                }
            }
        }
    }
}