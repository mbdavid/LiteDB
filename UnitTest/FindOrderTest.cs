using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    public class OrderObj
    {
        [BsonId]
        public int Id { get; set; }
        public string Text { get; set; }
    }

    [TestClass]
    public class FindOrderTest
    {
        [TestMethod]
        public void Find_Order()
        {
            using (var db = new LiteEngine(DB.Path()))
            {
                var col = db.GetCollection<OrderObj>("order");
                col.EnsureIndex("Text");

                col.Insert(new OrderObj { Id = 1, Text = "D" });
                col.Insert(new OrderObj { Id = 2, Text = "A" });
                col.Insert(new OrderObj { Id = 3, Text = "E" });
                col.Insert(new OrderObj { Id = 4, Text = "C" });
                col.Insert(new OrderObj { Id = 5, Text = "B" });

                var asc = col.Find(Query.All("Text"));
                var result = "";

                foreach (var i in asc)
                {
                    result += i.Text.ToString();
                }

                Assert.AreEqual("ABCDE", result);


            }
        }
    }
}
