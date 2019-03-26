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
            var page = new BasePage(buffer, 99, PageType.Collection);

            var index0 = page.GetFreeIndex();
            var slice0 = page.Insert(index0, 4);

            // first data slice must be just after header area
            Assert.AreEqual(Constants.PAGE_HEADER_SIZE + 10, slice0.Offset);

            // write (int)123 in this data slice
            slice0.Write(123, 0);

            Assert.AreEqual(123, slice0.ReadInt32(0));

            // adding slice1
            var index1 = page.GetFreeIndex();
            var slice1 = page.Insert(index1, 16);
            var guid = Guid.NewGuid();

            // second data slice must be just after header area + after first block (first slice use 6 bytes)
            Assert.AreEqual(Constants.PAGE_HEADER_SIZE + 10 + 4, slice1.Offset); // 4 for first block

            slice1.Write(guid, 0);

            Assert.AreEqual(guid, slice1.ReadGuid(0));

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
            Assert.AreEqual(PageType.Collection, page.PageType);

        }
    }
}