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
    public class HeaderPage_Tests
    {
        [TestMethod]
        public void HeaderPage_Collections()
        {
            var data = new byte[Constants.PAGE_SIZE];
            var buffer = new PageBuffer(data, 0, 1);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            // create new header page
            var header = new HeaderPage(buffer, 0);

            header.InsertCollection("my-col1", 1);
            header.InsertCollection("my-col2", 2);

            Assert.AreEqual(2, header.GetCollections().Count());
            Assert.AreEqual(1, (int)header.GetCollectionPageID("my-col1"));
            Assert.AreEqual(2, (int)header.GetCollectionPageID("my-col2"));
            Assert.AreEqual(7981, header.GetAvaiableCollectionSpace());

            header.UpdateBuffer();

            // read header
            var h2 = new HeaderPage(buffer);

            Assert.AreEqual(2, h2.GetCollections().Count());
            Assert.AreEqual(1, (int)h2.GetCollectionPageID("my-col1"));
            Assert.AreEqual(2, (int)h2.GetCollectionPageID("my-col2"));
            Assert.AreEqual(7981, h2.GetAvaiableCollectionSpace());

            buffer.ShareCounter = 0;
        }

        [TestMethod]
        public void HeaderPage_Savepoint()
        {
            var data = new byte[Constants.PAGE_SIZE];
            var buffer = new PageBuffer(data, 0, 1);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            // create new header page
            var header = new HeaderPage(buffer, 0);

            header.InsertCollection("my-col1", 1);
            header.InsertCollection("my-col2", 2);

            Assert.AreEqual(2, header.GetCollections().Count());

            // savepoint alse execute UpdateBuffer
            var sp = header.Savepoint();

            header.DeleteCollection("my-col1");
            header.DeleteCollection("my-col2");

            header.UpdateBuffer();

            Assert.AreEqual(0, header.GetCollections().Count());

            // now, restore header
            header.Restore(sp);

            Assert.AreEqual(2, header.GetCollections().Count());

            buffer.ShareCounter = 0;
        }
    }
}