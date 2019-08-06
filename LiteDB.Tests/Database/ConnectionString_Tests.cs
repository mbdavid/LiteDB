using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Database
{
    public class ConnectionString_Tests
    {
        [Fact]
        public void ConnectionString_Parser()
        {
            // only filename
            var onlyfile = new ConnectionString(@"demo.db");

            onlyfile.Filename.Should().Be(@"demo.db");

            // file with spaces without "
            var normal = new ConnectionString(@"filename=c:\only file\demo.db");

            normal.Filename.Should().Be(@"c:\only file\demo.db");

            // filename with timeout
            var filenameTimeout = new ConnectionString(@"filename = my demo.db ; timeout = 1:00:00");

            filenameTimeout.Filename.Should().Be(@"my demo.db");
            filenameTimeout.Timeout.Should().Be(TimeSpan.FromHours(1));

            // file with spaces with " and ;
            var full = new ConnectionString(
                @"filename=""c:\only;file\""d\""emo.db""; 
                  password =   ""john-doe "" ;
                  timeout = 300 ;
                  initial size = 10 MB ;
                  readONLY =  TRUE;
                  limit SIZE = 20mb;
                  utc=true");

            full.Filename.Should().Be(@"c:\only;file""d""emo.db");
            full.Password.Should().Be("john-doe ");
            full.ReadOnly.Should().BeTrue();
            full.Timeout.Should().Be(TimeSpan.FromMinutes(5));
            full.InitialSize.Should().Be(10 * 1024 * 1024);
            full.LimitSize.Should().Be(20 * 1024 * 1024);
            full.UtcDate.Should().BeTrue();

        }
    }
}