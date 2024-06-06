using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace LiteDB.Tests.Utils;

internal class FailingFactAttribute : FactAttribute
{
    public FailingFactAttribute(string reason = "The test demonstrates a bug that currently exists")
    {
        Skip = reason;
    }
}