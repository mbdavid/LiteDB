using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using LiteDB.Engine;
using Xunit;

namespace LiteDB.Internals
{
    public class Pragma_Tests
    {
        [Fact]
        public void Pragma_RunTests()
        {
            var data = new byte[Constants.PAGE_SIZE];
            var buffer = new PageBuffer(data, 0, 1);

            // mark buffer as writable (debug propose)
            buffer.ShareCounter = Constants.BUFFER_WRITABLE;

            // create new header page
            var header = new HeaderPage(buffer, 0);

            this.Invoking(x => header.Pragmas.Get("INEXISTENT_PRAGMA")).Should().Throw<Exception>();

            this.Invoking(x => header.Pragmas.Set("USER_VERSION", "invalid value", true)).Should().Throw<Exception>();
            this.Invoking(x => header.Pragmas.Set("USER_VERSION", 1, true)).Should().NotThrow();

            this.Invoking(x => header.Pragmas.Set("COLLATION", "en-US/IgnoreCase", true)).Should().Throw<Exception>();

            this.Invoking(x => header.Pragmas.Set("TIMEOUT", -1, true)).Should().Throw<Exception>();
            this.Invoking(x => header.Pragmas.Set("TIMEOUT", 1, true)).Should().NotThrow();

            this.Invoking(x => header.Pragmas.Set("LIMIT_SIZE", 1000, true)).Should().Throw<Exception>();
            this.Invoking(x => header.Pragmas.Set("LIMIT_SIZE", (Convert.ToInt32(header.LastPageID)) * Constants.PAGE_SIZE - 1, true)).Should().Throw<Exception>();
            this.Invoking(x => header.Pragmas.Set("LIMIT_SIZE", 1024L*1024L*1024L*1024L, true)).Should().NotThrow();

            this.Invoking(x => header.Pragmas.Set("UTC_DATE", true, true)).Should().NotThrow();

            this.Invoking(x => header.Pragmas.Set("CHECKPOINT", -1, true)).Should().Throw<Exception>();

            // MUST clear sharecount before finalize
            buffer.ShareCounter = 0;
        }
    }
}
