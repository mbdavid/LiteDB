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
        public void Insert_Bytes_In_BasePage()
        {
            // create new memory area with 10 bytes offset (just for fun)
            var data = new byte[Constants.PAGE_SIZE + 10];
            var buffer = new PageBuffer(data, 10, 1);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            // create new base page
            var page = new BasePage(buffer, 99, PageType.Empty);

            var seg0 = page.Insert(4, out var index0);

            // write (int)123 in this segment
            seg0.Write(123, 0);

            Assert.AreEqual(123, seg0.ReadInt32(0));

            // adding segment #1
            var seg1 = page.Insert(16, out var index1);
            var guid = Guid.NewGuid();

            // second data slice must be just after header area + after first block (first slice use 6 bytes)
            Assert.AreEqual(Constants.PAGE_HEADER_SIZE + 10 + 4, seg1.Offset); // 4 for first block

            seg1.Write(guid, 0);

            Assert.AreEqual(guid, seg1.ReadGuid(0));

            // update page buffer (header info
            page.GetBuffer(true);

            // testing if data was persist correctly in page buffer
            Assert.AreEqual(123, BitConverter.ToInt32(buffer.Array, 10 + Constants.PAGE_HEADER_SIZE));

            var guidBytes = new byte[16];
            Buffer.BlockCopy(buffer.Array, 10 + Constants.PAGE_HEADER_SIZE + 4, guidBytes, 0, 16);

            Assert.AreEqual(guid, new Guid(guidBytes));


            // let's create another page instance based on same page buffer
            var page2 = new BasePage(buffer);

            Assert.AreEqual(99, (int)page.PageID);
            Assert.AreEqual(PageType.Empty, page.PageType);

            buffer.ShareCounter = 0;

        }

        [TestMethod]
        public void Delete_Bytes_In_BasePage()
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

            // let's test defrag
            page.Defrag();

            Assert.AreEqual(32 + 100, page.NextFreePosition);
            Assert.AreEqual(1, page.HighestIndex);

            // ensure index0 will be reused
            page.Insert(1000, out var newIndex0);
            Assert.AreEqual(0, newIndex0);

            buffer.ShareCounter = 0;
        }

    }
}