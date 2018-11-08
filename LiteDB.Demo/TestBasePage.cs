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
    public class TestBasePage
    {
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
            var pageBuffer = new PageBuffer(new byte[8192], 0);
            var page = new BasePage(pageBuffer);
            var docLength = doc.GetBytesCount(true);

            //page.NewPage(25, PageType.Data);

            InsertDoc(page, doc, 0); // 0
            InsertDoc(page, doc, 1); // 1
            //InsertDoc(page, doc, 2); // 2
            //InsertDoc(page, doc, 3); // 3
            //InsertDoc(page, doc, 4); // 4 
            //InsertDoc(page, doc, 5); // 5

            doc["name"] = "NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018";

            UpdateDoc(page, 0, doc, 25);

            //page.Delete(0);
            //page.Delete(1);
            //page.Delete(2);
            //
            //InsertDoc(page, doc, 6); // 0
            //InsertDoc(page, doc, 7); // 1

            var d0 = ReadDoc(page, 0);

            page.Defrag();

            var d1 = ReadDoc(page, 1);

            //page.WriteHeader();

            ;

        }

        static void InsertDoc(BasePage page, BsonDocument doc, int id)
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

        static void UpdateDoc(BasePage page, byte index, BsonDocument doc, int id)
        {
            doc["_id"] = id;

            var len = doc.GetBytesCount(true);

            Console.WriteLine($"Doc Id: {id} - Length: {len}");

            var segment = page.Update(index, len);

            using (var writer = new BufferWriter(new[] { segment.Buffer }))
            {
                writer.WriteDocument(doc);
            }
        }

        static BsonDocument ReadDoc(BasePage page, byte index)
        {
            var segment = page.Get(index);

            using (var reader = new BufferReader(new[] { segment.Buffer }))
            {
                return reader.ReadDocument();
            }

        }
    }

}
