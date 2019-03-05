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

namespace LiteDB.Tests.Internals
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
            var p0 = new IndexPage(buffer, 99);

            var len0 = IndexNode.GetNodeLength(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");

            var s0 = p0.InsertNode(1, 3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", new PageAddress(98, 97), len0);

            s0.SetPrev(0, new PageAddress(1, 2));
            s0.SetNext(0, new PageAddress(3, 4));

            s0.SetPrev(1, new PageAddress(5, 6));
            s0.SetNext(1, new PageAddress(7, 8));

            s0.SetPrev(2, new PageAddress(9, 10));
            s0.SetNext(2, new PageAddress(11, 12));

            p0.GetBuffer(true);

            var len1 = IndexNode.GetNodeLength(1, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");

            var s1 = p0.InsertNode(1, 1, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", new PageAddress(98, 97), len1);

            s1.SetPrev(0, new PageAddress(1, 2));
            s1.SetNext(0, new PageAddress(3, 4));

            p0.GetBuffer(true);

            ;


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
