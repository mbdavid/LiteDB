using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public enum BsonExpressionType
    {
        Empty,
        Constant,
        Parameter,
        Array,
        Document,
        Call,
        Path,
        Conditional,
        Or,
        And
    }
}
