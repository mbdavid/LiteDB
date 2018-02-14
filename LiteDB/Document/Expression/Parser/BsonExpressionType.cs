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

        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual,

        Or,
        And
    }
}
