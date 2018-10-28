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
    class TestBasePage
    {
        static string PATH = @"d:\memory-file.db";

        static BsonDocument doc = new BsonDocument
        {
            ["_id"] = 1,
            ["name"] = "NoSQL Database",
            ["birthday"] = new DateTime(1977, 10, 30),
            ["phones"] = new BsonArray { "000000", "12345678" },
            ["active"] = true
        }; // 109b

        static void Main(string[] args)
        {
            var pageBuffer = new PageBuffer(new byte[8192], 0);
            var page = new BasePage2(pageBuffer);
            var docLength = doc.GetBytesCount(true);

            page.NewPage(25, PageType.Data);

            WriteDoc(page, doc, 0); // 0
            WriteDoc(page, doc, 1); // 1
            //WriteDoc(page, doc, 2); // 2
            //WriteDoc(page, doc, 3); // 3
            //WriteDoc(page, doc, 4); // 4 
            //WriteDoc(page, doc, 5); // 5

            page.Delete(0);
            //page.Delete(1);
            //page.Delete(2);
            //
            //WriteDoc(page, doc, 6); // 0
            //WriteDoc(page, doc, 7); // 1

            var d0 = ReadDoc(page, 1);

            page.Defrag();

            var d1 = ReadDoc(page, 1);

            page.UpdateHeaderBuffer();

            ;

        }

        static void WriteDoc(BasePage2 page, BsonDocument doc, int id)
        {
            doc["_id"] = id;

            var len = doc.GetBytesCount(true);

            Console.WriteLine($"Doc Id: {id} - Length: {len}");

            var segment = page.Insert(len);

            using (var writer = new BufferWriter(new[] { segment.Buffer }))
            {
                writer.WriteDocument(doc);
            }
        }

        static BsonDocument ReadDoc(BasePage2 page, byte index)
        {
            var segment = page.Get(index);

            using (var reader = new BufferReader(new[] { segment.Buffer }))
            {
                return reader.ReadDocument();
            }

        }
    }

}
