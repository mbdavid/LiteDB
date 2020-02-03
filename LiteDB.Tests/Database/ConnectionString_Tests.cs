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

            // file with spaces with " and ;
            var full = new ConnectionString(
                @"filename=""c:\only;file\""d\""emo.db""; 
                  password =   ""john-doe "" ;
                  initial size = 10 MB ;
                  readONLY =  TRUE;");

            full.Filename.Should().Be(@"c:\only;file""d""emo.db");
            full.Password.Should().Be("john-doe ");
            full.ReadOnly.Should().BeTrue();
            full.InitialSize.Should().Be(10 * 1024 * 1024);

        }
    }
}