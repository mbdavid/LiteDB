using FluentAssertions;
using LiteDB.Engine;
using System;
using System.IO;
using System.Linq;

using Xunit;

#if DEBUG
namespace LiteDB.Tests.Engine
{
    public class Rebuild_Loop_Tests
    {

        [Fact]
        public void Rebuild_Detected_Infinite_Loop()
        {
            var original = "../../../Resources/Loop.db";
            var filename = original.Replace(".db", "-copy");

            File.Copy(original, filename, true);

            var settings = new EngineSettings
            {
                Filename = filename,
                Password = "bzj2NplCbVH/bB8fxtjEC7u0unYdKHJVSmdmPgArRBwmmGw0+Wd2tE+b2zRMFcHAzoG71YIn/2Nq1EMqa5JKcQ==",
                AutoRebuild = true,
            };

            try
            {
                using (var db = new LiteEngine(settings))
                {
                    // infinite loop here
                    var col = db.Query("hubData$AppOperations", Query.All()).ToList();

                    // never run here
                    Assert.Fail("not expected");
                }
            }
            catch (Exception ex)
            {
                Assert.True(ex is LiteException lex && lex.ErrorCode == 999);
            }

            using (var db = new LiteEngine(settings))
            {
                var col = db.Query("hubData$AppOperations", Query.All()).ToList().Count;
                var errors = db.Query("_rebuild_errors", Query.All()).ToList().Count;

                col.Should().Be(408);
                errors.Should().Be(0);
            }
        }
    }
}

#endif
