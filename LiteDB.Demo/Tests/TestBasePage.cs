//using LiteDB;
//using LiteDB.Engine;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace LiteDB.Demo
//{
//    public class TestBasePage
//    {
//        static BsonDocument doc = new BsonDocument
//        {
//            ["_id"] = 1,
//            ["name"] = "NoSQL",
//            ["birthday"] = new DateTime(1977, 10, 30),
//            ["phones"] = new BsonArray { "000000", "12345678" },
//            ["active"] = true
//        }; // 100b

//        public static void Run(Stopwatch sw)
//        {
//            var buffer = new byte[8192];
//            var pageBuffer = new PageBuffer(buffer, 0) { ShareCounter = -1 };
//            var page = new BasePage2(pageBuffer, 0, PageType.Data);

//            InsertDoc(page, doc, 0); // 0
//            InsertDoc(page, doc, 1); // 1
//            //InsertDoc(page, doc, 2); // 2
//            //InsertDoc(page, doc, 3); // 3
//            //InsertDoc(page, doc, 4); // 4 
//            //InsertDoc(page, doc, 5); // 5

//            doc["name"] = "NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018-NoSQL 2018";

//            UpdateDoc(page, 0, doc, 25);

//            //page.Delete(0);
//            //page.Delete(1);
//            //page.Delete(2);
//            //
//            //InsertDoc(page, doc, 6); // 0
//            //InsertDoc(page, doc, 7); // 1

//            var d0 = ReadDoc(page, 0);

//            page.DefragSegments();

//            var d1 = ReadDoc(page, 1);

//            //page.WriteHeader();

//            ;

//        }

//        static void InsertDoc(BasePage2 page, BsonDocument doc, int id)
//        {
//            doc["_id"] = id;

//            var len = doc.GetBytesCount(true);

//            Console.WriteLine($"Doc Id: {id} - Length: {len}");

//            var segment = page.InsertSegment(len);

//            using (var writer = new BufferWriter(new[] { segment.Buffer }))
//            {
//                writer.WriteDocument(doc);
//            }
//        }

//        static void UpdateDoc(BasePage2 page, byte index, BsonDocument doc, int id)
//        {
//            doc["_id"] = id;

//            var len = doc.GetBytesCount(true);

//            Console.WriteLine($"Doc Id: {id} - Length: {len}");

//            var segment = page.UpdateSegment(index, len);

//            using (var writer = new BufferWriter(new[] { segment.Buffer }))
//            {
//                writer.WriteDocument(doc);
//            }
//        }

//        static BsonDocument ReadDoc(BasePage2 page, byte index)
//        {
//            var segment = page.GetSegment(index);

//            using (var reader = new BufferReader(new[] { segment.Buffer }))
//            {
//                return reader.ReadDocument();
//            }

//        }
//    }

//    /// <summary>
//    /// Implement protected as internal
//    /// </summary>
//    internal class BasePage2 : BasePage
//    {
//        public BasePage2(PageBuffer buffer) : base(buffer)
//        {
//        }

//        public BasePage2(PageBuffer buffer, uint pageID, PageType pageType) : base(buffer, pageID, pageType)
//        {
//        }

//        public PageSegment InsertSegment(int bytesLenth) => base.Insert(bytesLenth);
//        public PageSegment UpdateSegment(byte index, int bytesLenth) => base.Update(index, bytesLenth);
//        public PageSegment DeleteSegment(byte index) => base.Delete(index);
//        public PageSegment GetSegment(byte index) => base.Get(index);
//        public void DefragSegments() => base.Defrag();
//    }
//}
