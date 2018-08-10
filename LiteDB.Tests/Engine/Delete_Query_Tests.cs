using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Delete_Query_Tests
    {
        [TestMethod]
        public void Delete_Query()
        {
            using (var file = new TempFile())
            {
                var initial = new DateTime(2000, 01, 01);

                using (var db = new LiteEngine(file.Filename))
                {
                    for(var i = 0; i < 5000; i++)
                    {
                        db.Insert("col", new BsonDocument { { "dt", initial.AddDays(i) } });
                    }

                    db.EnsureIndex("col", "dt");

                    Assert.AreEqual(5000, db.Count("col"));

                    Assert.AreEqual(0, db.Count("col", Query.GT("dd", initial)));

                    var del = db.Delete("col", Query.GT("dd", initial));

                    Assert.AreEqual(0, del);

                    Assert.AreEqual(0, db.Count("col", Query.GT("dd", initial)));

                }
            }
        }
    }
}