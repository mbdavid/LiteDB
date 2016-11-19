using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace LiteDB.Tests
{
    [TestClass]
    public class ConnectionStringTest
    {
        [TestMethod]
        public void ConnectionString_Test()
        {
            // only filename
            var onlyfile = new ConnectionString(@"demo.db");

            Assert.AreEqual(@"demo.db", onlyfile.Filename);

            // file with spaces without "
            var normal = new ConnectionString(@"filename=c:\only file\demo.db; journal=false");

            Assert.AreEqual(@"c:\only file\demo.db", normal.Filename);
            Assert.AreEqual(false, normal.Journal);

            // filename with timeout
            var filenameTimeout = new ConnectionString(@"filename = my demo.db ; timeout = 1:00:00");

            Assert.AreEqual(@"my demo.db", filenameTimeout.Filename);
            Assert.AreEqual(TimeSpan.FromHours(1), filenameTimeout.Timeout);

            // file with spaces with " and ;
            var full = new ConnectionString(
                @"filename=""c:\only;file\""d\""emo.db""; 
                  journal =false;
                  password =   ""john-doe "" ;
                  cache size = 1000 ;
                  timeout = 00:05:00 ;
                  auto commit = true ;
                  initial size = 10 MB ;
                  limit size = 20mb;
                  log = 255");

            Assert.AreEqual(@"c:\only;file""d""emo.db", full.Filename);
            Assert.AreEqual(false, full.Journal);
            Assert.AreEqual("john-doe ", full.Password);
            Assert.AreEqual(1000, full.CacheSize);
            Assert.AreEqual(TimeSpan.FromMinutes(5), full.Timeout);
            Assert.AreEqual(true, full.AutoCommit);
            Assert.AreEqual(10 * 1024 * 1024, full.InitialSize);
            Assert.AreEqual(20 * 1024 * 1024, full.LimitSize);
            Assert.AreEqual(255, full.Log);

        }
    }
}