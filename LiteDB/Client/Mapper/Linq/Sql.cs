using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public static class Sql
    {
        public static T Items<T>(this IEnumerable<T> items)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }

        public static T Items<T>(this IEnumerable<T> items, int index)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }

        public static T Items<T>(this IEnumerable<T> items, Expression<Func<T, bool>> filter)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }

        public static int Count<T>(T values)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }

        public static T Sum<T>(T values)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }

        public static T Avg<T>(T values)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }

        public static T Min<T>(T values)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }

        public static T Max<T>(T values)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }

        public static T First<T>(T values)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }

        public static T Last<T>(T values)
        {
            throw new NotImplementedException("This method are used only for LINQ expression converter into BsonExpression");
        }
    }
}