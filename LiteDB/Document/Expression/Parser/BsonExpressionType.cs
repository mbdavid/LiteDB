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

        Double,
        Int,
        String,
        Boolean,
        Null,
        Array,
        Document,

        Parameter,
        Call,
        Path,

        Modulo,
        Add,
        Subtract,
        Multiply,
        Divide,

        Equal,
        StartsWith,
        Between,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        EndsWith,
        NotEqual,

        Or,
        And
    }
}
