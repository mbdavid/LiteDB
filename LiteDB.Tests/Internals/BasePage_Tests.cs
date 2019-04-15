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
    public class BasePage_Tests
    {
        [TestMethod]
        public void BasePage_Insert()
        {
            // create new memory area with 10 bytes offset (just for fun)
            var data = new byte[Constants.PAGE_SIZE + 10];
            var buffer = new PageBuffer(data, 10, 1);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            // create new base page
            var page = new BasePage(buffer, 1, PageType.Empty);

            page.Insert(10, out var index0).Fill(1);
            page.Insert(20, out var index1).Fill(2);
            page.Insert(30, out var index2).Fill(3);
            page.Insert(40, out var index3).Fill(4);

            Assert.AreEqual(0, page.FragmentedBytes);
            Assert.AreEqual(100, page.UsedBytes);
            Assert.AreEqual(32 + 100, page.NextFreePosition);
            Assert.AreEqual(1 + (4 * 4), page.FooterSize);
            Assert.AreEqual(8192 - 32 - 100 - (1 + (4 * 4)), page.FreeBytes);

            Assert.IsTrue(page.Get(index0).All(1));
            Assert.IsTrue(page.Get(index1).All(2));
            Assert.IsTrue(page.Get(index2).All(3));
            Assert.IsTrue(page.Get(index3).All(4));

            // update header buffer
            page.UpdateBuffer();

            // let's create another page instance based on same page buffer
            var page2 = new BasePage(buffer);

            Assert.AreEqual(1, (int)page.PageID);
            Assert.AreEqual(PageType.Empty, page.PageType);

            Assert.IsTrue(page.Get(index0).All(1));
            Assert.IsTrue(page.Get(index1).All(2));
            Assert.IsTrue(page.Get(index2).All(3));
            Assert.IsTrue(page.Get(index3).All(4));

            buffer.ShareCounter = 0;
        }

        [TestMethod]
        public void BasePage_Insert_Full_Bytes_Page()
        {
            // create new memory area
            var data = new byte[Constants.PAGE_SIZE];
            var buffer = new PageBuffer(data, 0, 1);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            // create new base page
            var page = new BasePage(buffer, 1, PageType.Empty);

            var full = page.FreeBytes - BasePage.SLOT_SIZE;

            page.Insert((ushort)full, out var index0).Fill(1);

            Assert.AreEqual(1, page.ItemsCount);
            Assert.AreEqual(full, page.UsedBytes);
            Assert.AreEqual(0, page.FreeBytes);
            Assert.AreEqual(32 + full, page.NextFreePosition);

            buffer.ShareCounter = 0;
        }

        [TestMethod]
        public void BasePage_Insert_Full_Items_Page()
        {
            // create new memory area
            var data = new byte[Constants.PAGE_SIZE];
            var buffer = new PageBuffer(data, 0, 1);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            // create new base page
            var page = new BasePage(buffer, 1, PageType.Empty);

            // create 255 page segments
            for(byte i = 0; i < byte.MaxValue; i++)
            {
                page.Insert(10, out var index).Fill(i);
            }

            Assert.AreEqual(255, page.ItemsCount);
            Assert.AreEqual(2550, page.UsedBytes);
            Assert.AreEqual(0, page.FreeBytes);
            Assert.AreEqual(32 + 2550, page.NextFreePosition);

            buffer.ShareCounter = 0;
        }

        [TestMethod]
        public void BasePage_Delete()
        {
            var data = new byte[Constants.PAGE_SIZE];
            var buffer = new PageBuffer(data, 0, 0);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            // create new base page
            var page = new BasePage(buffer, 1, PageType.Empty);

            var seg0 = page.Insert(100, out var index0);
            var seg1 = page.Insert(200, out var index1);
            var seg2 = page.Insert(300, out var index2);

            Assert.AreEqual(2, page.HighestIndex);
            Assert.AreEqual(3, page.ItemsCount);
            Assert.AreEqual(600, page.UsedBytes);
            Assert.AreEqual(32 + 600, page.NextFreePosition);
            Assert.AreEqual(8192 - 32 - 13 - 600, page.FreeBytes); // page size - header - footer - used
            Assert.AreEqual(0, page.FragmentedBytes);

            // deleting 300b (end of page)
            page.Delete(index2); 

            Assert.AreEqual(1, page.HighestIndex);
            Assert.AreEqual(2, page.ItemsCount);
            Assert.AreEqual(300, page.UsedBytes);
            Assert.AreEqual(32 + 300, page.NextFreePosition);
            Assert.AreEqual(8192 - 32 - 9 - 300, page.FreeBytes);
            Assert.AreEqual(0, page.FragmentedBytes);

            // deleting 100b (middle of page) - create data fragment
            page.Delete(index0); 

            Assert.AreEqual(1, page.HighestIndex);
            Assert.AreEqual(1, page.ItemsCount);
            Assert.AreEqual(200, page.UsedBytes);
            Assert.AreEqual(32 + 300, page.NextFreePosition); // 200 + 100 (fragmented)
            Assert.AreEqual(8192 - 32 - 9 - 200, page.FreeBytes);
            Assert.AreEqual(100, page.FragmentedBytes);

            // delete 200b - last item
            page.Delete(index1);

            // after delete last item page will be defrag

            Assert.AreEqual(byte.MaxValue, page.HighestIndex);
            Assert.AreEqual(0, page.ItemsCount);
            Assert.AreEqual(0, page.UsedBytes);
            Assert.AreEqual(32, page.NextFreePosition);
            Assert.AreEqual(8192 - 32 - 1, page.FreeBytes);
            Assert.AreEqual(0, page.FragmentedBytes);

            buffer.ShareCounter = 0;
        }

        [TestMethod]
        public void BasePage_Defrag()
        {
            var data = new byte[Constants.PAGE_SIZE];
            var buffer = new PageBuffer(data, 0, 0);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            var page = new BasePage(buffer, 1, PageType.Empty);

            page.Insert(100, out var index0).Fill(101);
            page.Insert(200, out var index1).Fill(102);
            page.Insert(300, out var index2).Fill(103);
            page.Insert(400, out var index3).Fill(104);

            Assert.AreEqual(0, page.FragmentedBytes);
            Assert.AreEqual(1000, page.UsedBytes);
            Assert.AreEqual(32 + 1000, page.NextFreePosition);

            page.Delete(index0);
            page.Delete(index1);

            Assert.AreEqual(300, page.FragmentedBytes);
            Assert.AreEqual(700, page.UsedBytes);
            Assert.AreEqual(32 + 1000, page.NextFreePosition);

            page.Defrag();

            Assert.AreEqual(0, page.FragmentedBytes);
            Assert.AreEqual(700, page.UsedBytes);
            Assert.AreEqual(32 + 700, page.NextFreePosition);

            Assert.IsTrue(page.Get(index2).All(103));
            Assert.IsTrue(page.Get(index3).All(104));

            CollectionAssert.AreEqual(new byte[] { 2, 3 }, page.GetUsedIndexs().ToArray());

            buffer.ShareCounter = 0;
        }

        [TestMethod]
        public void BasePage_Update()
        {
            var data = new byte[Constants.PAGE_SIZE];
            var buffer = new PageBuffer(data, 0, 0);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            var page = new BasePage(buffer, 1, PageType.Empty);

            page.Insert(100, out var index0).Fill(101);
            page.Insert(200, out var index1).Fill(102);
            page.Insert(300, out var index2).Fill(103);
            page.Insert(400, out var index3).Fill(104);

            Assert.AreEqual(0, page.FragmentedBytes);
            Assert.AreEqual(1000, page.UsedBytes);
            Assert.AreEqual(32 + 1000, page.NextFreePosition);

            // update same segment length 
            page.Update(index0, 100).Fill(201);

            Assert.IsTrue(page.Get(index0).All(201));
            Assert.AreEqual(0, page.FragmentedBytes);
            Assert.AreEqual(1000, page.UsedBytes);
            Assert.AreEqual(32 + 1000, page.NextFreePosition);

            // less bytes (segment in middle of page)
            page.Update(index1, 150).Fill(202);

            Assert.IsTrue(page.Get(index1).All(202));
            Assert.AreEqual(50, page.FragmentedBytes);
            Assert.AreEqual(950, page.UsedBytes);
            Assert.AreEqual(32 + 1000, page.NextFreePosition);

            // less bytes (segment in end of page)
            page.Update(index3, 350).Fill(204);

            Assert.IsTrue(page.Get(index3).All(204));
            Assert.AreEqual(50, page.FragmentedBytes);
            Assert.AreEqual(900, page.UsedBytes);
            Assert.AreEqual(32 + 950, page.NextFreePosition);

            // more bytes (segment in end of page)
            page.Update(index3, 550).Fill(214);

            Assert.IsTrue(page.Get(index3).All(214));
            Assert.AreEqual(50, page.FragmentedBytes);
            Assert.AreEqual(1100, page.UsedBytes);
            Assert.AreEqual(32 + 1150, page.NextFreePosition);

            // more bytes (segment in middle of page)
            page.Update(index0, 200).Fill(211);

            Assert.IsTrue(page.Get(index0).All(211));
            Assert.AreEqual(150, page.FragmentedBytes);
            Assert.AreEqual(1200, page.UsedBytes);
            Assert.AreEqual(32 + 1350, page.NextFreePosition);

            buffer.ShareCounter = 0;
        }
    }
}