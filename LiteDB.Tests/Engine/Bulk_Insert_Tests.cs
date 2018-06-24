using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LiteDB.Engine;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Bulk_Insert_Tests
    {
        [TestMethod]
        public void Bulk_Insert_Engine()
        {
            var data = DataGen.Person(20, 10).ToArray();

            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.Insert("col", data);

                var result = db.Find("col", Query.LT("age", 10)).Count();

                Assert.Equals(1, result);



            }
        }
    }
}