using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;
using System.Threading;

namespace LiteDB.Internals
{
    [TestClass]
    public class FreePage_Tests
    {
        // FreeBytes ranges on page slot for free list page
        // 90% - 100% = 0 (7344 - 8160)
        // 75% -  90% = 1 (6120 - 7343)
        // 60% -  75% = 2 (4896 - 6119)
        // 30% -  60% = 3 (2448 - 4895)
        //  0% -  30% = 4 (0000 - 2447)

        [TestMethod]
        public void FreeSlot_Insert()
        {
            using (var e = new LiteEngine())
            {
                e.BeginTrans();

                // get transaction/snapshot "col1"
                var t = e.GetTransaction(false, out var isNew);
                var s = t.CreateSnapshot(LockMode.Write, "col1", true);

                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["n"] = new byte[200] } }, BsonAutoId.Int32);

                // get pages
                var colPage = s.CollectionPage;
                var dataPage = s.LocalPages.FirstOrDefault(x => x.PageType == PageType.Data);
                var indexPage = s.LocalPages.FirstOrDefault(x => x.PageType == PageType.Index);

                // test dataPage free space
                Assert.AreEqual(7927, dataPage.FreeBytes);

                // page sloud be in Slot #0 (7344 - 8160 free bytes)
                CollectionAssert.AreEqual(new uint[] { dataPage.PageID, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue },
                    colPage.FreeDataPageID);

                // adding 1 more document into same page
                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["n"] = new byte[600] } }, BsonAutoId.Int32);

                Assert.AreEqual(7295, dataPage.FreeBytes);

                // page should me moved into Slot #1 (6120 - 7343 free bytes) 
                CollectionAssert.AreEqual(new uint[] { uint.MaxValue, dataPage.PageID, uint.MaxValue, uint.MaxValue, uint.MaxValue },
                    colPage.FreeDataPageID);

                // adding 1 big document to move this page into last page
                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["n"] = new byte[6000] } }, BsonAutoId.Int32);

                Assert.AreEqual(1263, dataPage.FreeBytes);

                // now this page should me moved into last Slot (#4) - next document will use another data page (even a very small document)
                CollectionAssert.AreEqual(new uint[] { uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, dataPage.PageID },
                    colPage.FreeDataPageID);

                // adding a very small document to test adding new page
                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["n"] = new byte[10] } }, BsonAutoId.Int32);

                // no changes in dataPage... but new page as created
                Assert.AreEqual(1263, dataPage.FreeBytes);

                var dataPage2 = s.LocalPages.FirstOrDefault(x => x.PageType == PageType.Data && x.PageID != dataPage.PageID);

                Assert.AreEqual(8117, dataPage2.FreeBytes);

                // test slots (#0 for dataPage2 and #4 for dataPage1)
                CollectionAssert.AreEqual(new uint[] { dataPage2.PageID, uint.MaxValue, uint.MaxValue, uint.MaxValue, dataPage.PageID },
                    colPage.FreeDataPageID);

                // add another big document into dataPage2 do put both pages in same free Slot (#4)
                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["n"] = new byte[7000] } }, BsonAutoId.Int32);

                // now, both pages are linked in same slot #4 (starts with new dataPage2)
                CollectionAssert.AreEqual(new uint[] { uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, dataPage2.PageID },
                    colPage.FreeDataPageID);

                // dataPage2 link into dataPage1
                Assert.AreEqual(dataPage.PageID, dataPage2.NextPageID);
                Assert.AreEqual(dataPage2.PageID, dataPage.PrevPageID);

                // and both start/end points to null
                Assert.AreEqual(uint.MaxValue, dataPage2.PrevPageID);
                Assert.AreEqual(uint.MaxValue, dataPage.NextPageID);

                // do ColID tests
                Assert.AreEqual(colPage.PageID, dataPage.ColID);
                Assert.AreEqual(colPage.PageID, dataPage2.ColID);
                Assert.AreEqual(colPage.PageID, indexPage.ColID);
            }
        }

        [TestMethod]
        public void FreeSlot_Delete()
        {
            using (var e = new LiteEngine())
            {
                e.BeginTrans();

                // get transaction/snapshot "col1"
                var t = e.GetTransaction(false, out var isNew);
                var s = t.CreateSnapshot(LockMode.Write, "col1", true);

                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["n"] = new byte[200] } }, BsonAutoId.Int32);

                // get pages
                var colPage = s.CollectionPage;
                var dataPage = s.LocalPages.FirstOrDefault(x => x.PageType == PageType.Data);
                var indexPage = s.LocalPages.FirstOrDefault(x => x.PageType == PageType.Index);

                // test dataPage free space
                Assert.AreEqual(7927, dataPage.FreeBytes);

                // page sloud be in Slot #0 (7344 - 8160 free bytes)
                CollectionAssert.AreEqual(new uint[] { dataPage.PageID, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue },
                    colPage.FreeDataPageID);

                // adding 1 more document into same page
                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["n"] = new byte[600] } }, BsonAutoId.Int32);

                Assert.AreEqual(7295, dataPage.FreeBytes);

                // page should me moved into Slot #1 (6120 - 7343 free bytes) 
                CollectionAssert.AreEqual(new uint[] { uint.MaxValue, dataPage.PageID, uint.MaxValue, uint.MaxValue, uint.MaxValue },
                    colPage.FreeDataPageID);

                // adding 1 big document to move this page into last page
                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["n"] = new byte[6000] } }, BsonAutoId.Int32);

                Assert.AreEqual(1263, dataPage.FreeBytes);

                // now this page should me moved into last Slot (#4) - next document will use another data page (even a very small document)
                CollectionAssert.AreEqual(new uint[] { uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, dataPage.PageID },
                    colPage.FreeDataPageID);

                // adding a very small document to test adding new page
                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["n"] = new byte[10] } }, BsonAutoId.Int32);

                // no changes in dataPage... but new page as created
                Assert.AreEqual(1263, dataPage.FreeBytes);

                var dataPage2 = s.LocalPages.FirstOrDefault(x => x.PageType == PageType.Data && x.PageID != dataPage.PageID);

                Assert.AreEqual(8117, dataPage2.FreeBytes);

                // test slots (#0 for dataPage2 and #4 for dataPage1)
                CollectionAssert.AreEqual(new uint[] { dataPage2.PageID, uint.MaxValue, uint.MaxValue, uint.MaxValue, dataPage.PageID },
                    colPage.FreeDataPageID);

                // do ColID tests
                Assert.AreEqual(colPage.PageID, dataPage.ColID);
                Assert.AreEqual(colPage.PageID, dataPage2.ColID);
                Assert.AreEqual(colPage.PageID, indexPage.ColID);
            }
        }
    }
}