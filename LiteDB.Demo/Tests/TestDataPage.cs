using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class TestDataPage
    {
        static string PATH = @"D:\memory-file.db";
        static BsonDocument doc = new BsonDocument
        {
            ["_id"] = 1,
            ["name"] = "NoSQL Database",
            ["birthday"] = new DateTime(1977, 10, 30),
            ["phones"] = new BsonArray { "000000", "12345678" },
            ["active"] = true
        }; // 109b

        public static void Run(Stopwatch sw)
        {
            File.Delete(PATH);

            var buffer = new PageBuffer(new byte[8192], 0, 0) { ShareCounter = -1 };
            var p0 = new IndexPage(buffer, 0);

            var ss = p0.InsertNode(3, "okok", new PageAddress(123, 25));

            ss.SetNext(2, new PageAddress(1, 1));


        }

        private static void Write(DataBlock block, BsonDocument doc)
        {
            using (var w = new BufferWriter(block.Buffer))
            {
                w.WriteDocument(doc);
            }
        }
    }
}
