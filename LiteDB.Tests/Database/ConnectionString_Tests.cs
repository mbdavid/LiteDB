using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Tests.Database
{
    [TestClass]
    public class ConnectionString_Tests
    {
        [TestMethod]
        public void ConnectionString_NoArguments()
        {
            // Verify that the default ConnectionString contains the appropriate defaults
            var defaults = new ConnectionString();
            AssertDefaults(defaults, false);
        }

        [TestMethod]
        public void ConnectionString_Passwords()
        {
            var parsed = new ConnectionString(@"filename=demo;password=""ab\""|    c;785;""");
            Assert.AreEqual("demo", parsed.Filename);
            Assert.AreEqual(@"ab""|    c;785;", parsed.Password);

            var nonParsed = new ConnectionString
            {
                Filename = "demo",
                Password = @"ab""|    c;785;"
            };

            Assert.AreEqual(parsed.Filename, nonParsed.Filename);
            Assert.AreEqual(parsed.Password, nonParsed.Password);

            parsed = new ConnectionString(@"filename=demo;password=ab\""123");
            Assert.AreEqual(@"ab""123", parsed.Password);


            parsed = new ConnectionString(@"filename=demo;password=""ab;123"";");
            Assert.AreEqual(@"ab;123", parsed.Password);

            Assert.ThrowsException<FormatException>(() => new ConnectionString(@"filename=demo;password=ab;123"));
        }

        [TestMethod]
        public void ConnectionString_Bools()
        {
            var parsed = new ConnectionString("filename=demo.db; jOuRnal  =  FALSE  ; upgrade=True;async =true;");
            Assert.AreEqual(false, parsed.Journal);
            Assert.AreEqual(true, parsed.Upgrade);
            Assert.AreEqual(true, parsed.Async);

            parsed = new ConnectionString("filename=demo.db; journal=true; upgrade=false;async =false");
            Assert.AreEqual(true, parsed.Journal);
            Assert.AreEqual(false, parsed.Upgrade);
            Assert.AreEqual(false, parsed.Async);

            Assert.ThrowsException<FormatException>(() => new ConnectionString("filename=demo.db; journal=string"));
        }

        [TestMethod]
        public void ConnectionString_Ints()
        {
            var parsed = new ConnectionString("filename=one;cache size = 1234");
            Assert.AreEqual(1234, parsed.CacheSize);
            
            parsed = new ConnectionString(@"filename=one;cache size = ""8"";");
            Assert.AreEqual(8, parsed.CacheSize);
            
            Assert.ThrowsException<FormatException>(() => new ConnectionString("filename=one;cache size = 8s;"));
        }

        [TestMethod]
        public void ConnectionString_TimeSpan()
        {
            var parsed = new ConnectionString("filename=one;Timeout = 12:34:56");
            Assert.AreEqual(new TimeSpan(12, 34, 56), parsed.Timeout);

            parsed = new ConnectionString(@"filename=one;Timeout =""12:34:56""");
            Assert.AreEqual(new TimeSpan(12, 34, 56), parsed.Timeout);
            
            Assert.ThrowsException<FormatException>(() => new ConnectionString("filename=one;Timeout = 12:234:56"));
        }

        [TestMethod]
        public void ConnectionString_Sizes()
        {
            var parsed = new ConnectionString(@"filename=one;initial   size=1234 ;LIMIT size = ""852""");
            Assert.AreEqual(1234, parsed.InitialSize);
            Assert.AreEqual(852, parsed.LimitSize);

            parsed = new ConnectionString(@"filename=one;initial   size=1234  mb ;LIMIT size = ""852KB""");
            Assert.AreEqual(1234 * 1024 * 1024, parsed.InitialSize);
            Assert.AreEqual(852 * 1024, parsed.LimitSize);

            parsed = new ConnectionString(@"filename=one;initial   size=  8  Gb ;LIMIT size =  ""28 KB""");
            Assert.AreEqual(8L * 1024 * 1024 * 1024, parsed.InitialSize);
            Assert.AreEqual(28 * 1024, parsed.LimitSize);

            Assert.ThrowsException<FormatException>(() =>
                new ConnectionString(@"filename=one;initial   size=1234p ;LIMIT size = ""852"""));

            Assert.ThrowsException<FormatException>(() =>
                new ConnectionString(@"filename=one;initial   size=1234 ;LIMIT size = ""852]"""));
        }

        [TestMethod]
        public void ConnectionString_InvalidSettings()
        {
            Assert.ThrowsException<FormatException>(()=> new ConnectionString(@"NoSuchSetting=one"));
            Assert.ThrowsException<FormatException>(() => new ConnectionString(@"filename=one;nosuchsetting=two"));
            
            // need filename
            Assert.ThrowsException<FormatException>(() => new ConnectionString(@"password=one"));
        }

        [TestMethod]
        public void ConnectionString_Parser()
        {
            // only filename
            var onlyfile = new ConnectionString(@"demo.db");

            Assert.AreEqual(@"demo.db", onlyfile.Filename);
            AssertDefaults(onlyfile, true);

            // file with spaces without "
            var normal = new ConnectionString(@"filename=c:\only file\demo.db; journal=false");

            Assert.AreEqual(@"c:\only file\demo.db", normal.Filename);
            Assert.AreEqual(false, normal.Journal);

            // filename with timeout
            var filenameTimeout = new ConnectionString(@"filename = my demo.db ; timeout = 1:00:00");

            Assert.AreEqual(@"my demo.db", filenameTimeout.Filename);
            Assert.AreEqual(TimeSpan.FromHours(1), filenameTimeout.Timeout);

            // file with spaces with " and ;
            var fullConnectionString = @"filename=""c:\only;file\""d\""emo.db""; 
                  journal =false;
                  password =   ""john-doe "" ;
                  cache SIZE = 1000 ;
                  timeout = 00:05:00 ;
                  initial size = 10 MB ;
                  mode =  excluSIVE ;
                  limit SIZE = 20mb;
                  log = 255 ;
                  utc=true ;
                  upgrade=true;
                  async=true";
            var full = new ConnectionString(fullConnectionString);

            Assert.AreEqual(@"c:\only;file""d""emo.db", full.Filename);
            Assert.AreEqual(false, full.Journal);
            Assert.AreEqual("john-doe ", full.Password);
            Assert.AreEqual(1000, full.CacheSize);
            Assert.AreEqual(FileMode.Exclusive, full.Mode);
            Assert.AreEqual(TimeSpan.FromMinutes(5), full.Timeout);
            Assert.AreEqual(10 * 1024 * 1024, full.InitialSize);
            Assert.AreEqual(20 * 1024 * 1024, full.LimitSize);
            Assert.AreEqual(255, full.Log);
            Assert.AreEqual(true, full.UtcDate);
            Assert.AreEqual(true, full.Upgrade);
            Assert.AreEqual(true, full.Async);
        }

        [TestMethod]
        public void ConnectionString_Sets_Log_Level()
        {
            var connectionString = "filename=foo;";
            var db = new LiteDatabase(connectionString);
            Assert.AreEqual(0, db.Log.Level);

            connectionString = "filename=foo;log=" + Logger.FULL;
            db = new LiteDatabase(connectionString);
            Assert.AreEqual(Logger.FULL, db.Log.Level);
        }

        [TestMethod]
        public void ConnectionString_MetaTest()
        {
            // This test is a meta test that verifies that all of the properties present in ConnectionString are also tested by this test.
            // If this test fails, you should make sure that you don't need to update this test. (In particular, you almost certainly need to update AssertDefaults.)
            var expectedProperties = new HashSet<string>()
            {
                "Filename",
                "Journal",
                "Password",
                "CacheSize",
                "Timeout",
                "Mode",
                "InitialSize",
                "LimitSize",
                "Log",
                "UtcDate",
                "Upgrade",
                "Async",
                "Flush"
            };

            var actualProperties = new HashSet<string>(typeof(ConnectionString).GetProperties().Select(p => p.Name));
            actualProperties.ExceptWith(expectedProperties);
            
            // If the below assert fails, properties were added to ConnectionString without updating this test.
            Assert.AreEqual(0, actualProperties.Count);
        }

        private void AssertDefaults(ConnectionString connectionString, bool skipFilename)
        {
            if (!skipFilename)
            { Assert.AreEqual("", connectionString.Filename); }

            Assert.AreEqual(true, connectionString.Journal);
            Assert.AreEqual(null, connectionString.Password);
            Assert.AreEqual(5000, connectionString.CacheSize);
            Assert.AreEqual(new TimeSpan(0, 1, 0), connectionString.Timeout);
            Assert.AreEqual(FileMode.Shared, connectionString.Mode);
            Assert.AreEqual(0, connectionString.InitialSize);
            Assert.AreEqual(long.MaxValue, connectionString.LimitSize);
            Assert.AreEqual(Logger.NONE, connectionString.Log);
            Assert.AreEqual(false, connectionString.UtcDate);
            Assert.AreEqual(false, connectionString.Upgrade);
            Assert.AreEqual(false, connectionString.Async);
        }
    }
}
