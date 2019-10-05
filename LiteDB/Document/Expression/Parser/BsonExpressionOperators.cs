using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB
{
    internal class BsonExpressionOperators
    {
        #region Arithmetic

        /// <summary>
        /// Add two number values. If any side are string, concat left+right as string
        /// </summary>
        public static BsonValue ADD(BsonValue left, BsonValue right)
        {
            // if both sides are string, concat
            if (left.IsString && right.IsString)
            {
                return left.AsString + right.AsString;
            }
            // if any sides are string, concat casting both to string
            else if (left.IsString || right.IsString)
            {
                return BsonExpressionMethods.STRING(left).AsString + BsonExpressionMethods.STRING(right).AsString;
            }
            // if any side are DateTime and another is number, add days in date
            else if (left.IsDateTime && right.IsNumber)
            {
                return left.AsDateTime.AddDays(right.AsDouble);
            }
            else if (left.IsNumber && right.IsDateTime)
            {
                return right.AsDateTime.AddDays(left.AsDouble);
            }
            // if both sides are number, add as math
            else if (left.IsNumber && right.IsNumber)
            {
                return left + right;
            }

            return BsonValue.Null;
        }

        /// <summary>
        /// Minus two number values
        /// </summary>
        public static BsonValue MINUS(BsonValue left, BsonValue right)
        {
            if (left.IsDateTime && right.IsNumber)
            {
                return left.AsDateTime.AddDays(-right.AsDouble);
            }
            else if (left.IsNumber && right.IsDateTime)
            {
                return right.AsDateTime.AddDays(-left.AsDouble);
            }
            else if (left.IsNumber && right.IsNumber)
            {
                return left - right;
            }

            return BsonValue.Null;
        }

        /// <summary>
        /// Multiply two number values
        /// </summary>
        public static BsonValue MULTIPLY(BsonValue left, BsonValue right)
        {
            if (left.IsNumber && right.IsNumber)
            {
                return left * right;
            }

            return BsonValue.Null;
        }

        /// <summary>
        /// Divide two number values
        /// </summary>
        public static BsonValue DIVIDE(BsonValue left, BsonValue right)
        {
            if (left.IsNumber && right.IsNumber)
            {
                return left / right;
            }

            return BsonValue.Null;
        }

        /// <summary>
        /// Mod two number values
        /// </summary>
        public static BsonValue MOD(BsonValue left, BsonValue right)
        {
            if (left.IsNumber && right.IsNumber)
            {
                return left % right;
            }

            return BsonValue.Null;
        }

        #endregion

        #region Predicates

        /// <summary>
        /// Test if left and right are same value. Returns true or false
        /// </summary>
        public static BsonValue EQ(BsonValue left, BsonValue right) => left == right;
        public static BsonValue EQ_ANY(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x == right);
        public static BsonValue EQ_ALL(IEnumerable<BsonValue> left, BsonValue right) => left.All(x => x == right);

        /// <summary>
        /// Test if left is greater than right value. Returns true or false
        /// </summary>
        public static BsonValue GT(BsonValue left, BsonValue right) => left > right;
        public static BsonValue GT_ANY(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x > right);
        public static BsonValue GT_ALL(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x > right);

        /// <summary>
        /// Test if left is greater or equals than right value. Returns true or false
        /// </summary>
        public static BsonValue GTE(BsonValue left, BsonValue right) => left >= right;
        public static BsonValue GTE_ANY(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x >= right);
        public static BsonValue GTE_ALL(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x >= right);


        /// <summary>
        /// Test if left is less than right value. Returns true or false
        /// </summary>
        public static BsonValue LT(BsonValue left, BsonValue right) => left < right;
        public static BsonValue LT_ANY(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x < right);
        public static BsonValue LT_ALL(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x < right);

        /// <summary>
        /// Test if left is less or equals than right value. Returns true or false
        /// </summary>
        public static BsonValue LTE(BsonValue left, BsonValue right) => left <= right;
        public static BsonValue LTE_ANY(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x <= right);
        public static BsonValue LTE_ALL(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x <= right);

        /// <summary>
        /// Test if left and right are not same value. Returns true or false
        /// </summary>
        public static BsonValue NEQ(BsonValue left, BsonValue right) => left != right;
        public static BsonValue NEQ_ANY(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x != right);
        public static BsonValue NEQ_ALL(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => x != right);

        /// <summary>
        /// Test if left is "SQL LIKE" with right. Returns true or false. Works only when left and right are string
        /// </summary>
        public static BsonValue LIKE(BsonValue left, BsonValue right)
        {
            if (left.IsString && right.IsString)
            {
                return left.AsString.SqlLike(right.AsString);
            }
            else
            {
                return false;
            }
        }

        public static BsonValue LIKE_ANY(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => LIKE(x, right));
        public static BsonValue LIKE_ALL(IEnumerable<BsonValue> left, BsonValue right) => left.All(x => LIKE(x, right));

        /// <summary>
        /// Test if left is between right-array. Returns true or false. Right value must be an array. Support multiple values
        /// </summary>
        public static BsonValue BETWEEN(BsonValue left, BsonValue right)
        {
            if (!right.IsArray) throw new InvalidOperationException("BETWEEN expression need an array with 2 values");

            var arr = right.AsArray;

            if (arr.Count != 2) throw new InvalidOperationException("BETWEEN expression need an array with 2 values");

            var start = arr[0];
            var end = arr[1];

            return left >= start && left <= end;
        }

        public static BsonValue BETWEEN_ANY(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => BETWEEN(x, right));
        public static BsonValue BETWEEN_ALL(IEnumerable<BsonValue> left, BsonValue right) => left.All(x => BETWEEN(x, right));

        /// <summary>
        /// Test if left are in any value in right side (when right side is an array). If right side is not an array, just implement a simple Equals (=). Returns true or false
        /// </summary>
        public static BsonValue IN(BsonValue left, BsonValue right)
        {
            if (right.IsArray)
            {
                return right.AsArray.Contains(left);
            }
            else
            {
                return left == right;
            }
        }

        public static BsonValue IN_ANY(IEnumerable<BsonValue> left, BsonValue right) => left.Any(x => IN(x, right));
        public static BsonValue IN_ALL(IEnumerable<BsonValue> left, BsonValue right) => left.All(x => IN(x, right));

        #endregion

        #region Logic

        /// <summary>
        /// Test left AND right value. Returns true or false
        /// </summary>
        public static BsonValue AND(BsonValue left, BsonValue right)
        {
            return left && right;
        }

        /// <summary>
        /// Test left OR right value. Returns true or false
        /// </summary>
        public static BsonValue OR(BsonValue left, BsonValue right)
        {
            return left || right;
        }

        #endregion

        #region Path Navigation

        /// <summary>
        /// Returns value from root document (used in parameter). Returns same document if name are empty
        /// </summary>
        public static BsonValue PARAMETER_PATH(BsonDocument doc, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return doc;
            }

            if (doc.TryGetValue(name, out BsonValue item))
            {
                return item;
            }
            else
            {
                return BsonValue.Null;
            }
        }

        /// <summary>
        /// Return a value from a value as document. If has no name, just return values ($). If value are not a document, do not return anything
        /// </summary>
        public static BsonValue MEMBER_PATH(BsonValue value, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return value;
            }
            else if (value.IsDocument)
            {
                var doc = value.AsDocument;

                if (doc.TryGetValue(name, out BsonValue item))
                {
                    return item;
                }
            }

            return BsonValue.Null;
        }

        #endregion

        #region Array Index/Filter

        /// <summary>
        /// Returns a single value from array according index or expression parameter
        /// </summary>
        public static BsonValue ARRAY_INDEX(BsonValue value, int index, BsonExpression expr, BsonDocument root, BsonDocument parameters)
        {
            if (!value.IsArray) return BsonValue.Null;

            var arr = value.AsArray;

            // for expr.Type = parameter, just get value as index (fixed position)
            if (expr.Type == BsonExpressionType.Parameter)
            {
                // update parameters in expression
                parameters.CopyTo(expr.Parameters);

                // get fixed position based on parameter value (must return int value)
                var indexValue = expr.ExecuteScalar(root);

                if (!indexValue.IsNumber) throw new LiteException(0, "Parameter expression must return number when called inside an array");

                index = indexValue.AsInt32;
            }

            var idx = index < 0 ? arr.Count + index : index;

            if (arr.Count > idx)
            {
                return arr[idx];
            }

            return BsonValue.Null;
        }

        /// <summary>
        /// Returns all values from array according filter expression or all values (index = MaxValue)
        /// </summary>
        public static IEnumerable<BsonValue> ARRAY_FILTER(BsonValue value, int index, BsonExpression filterExpr, BsonDocument root, BsonDocument parameters)
        {
            if (!value.IsArray) yield break;

            var arr = value.AsArray;

            // [*] - index are all values
            if (index == int.MaxValue)
            {
                foreach (var item in arr)
                {
                    yield return item;
                }
            }
            // [<expr>] - index are an expression
            else
            {
                // update parameters in expression
                parameters.CopyTo(filterExpr.Parameters);

                foreach (var item in arr)
                {
                    // execute for each child value and except a first bool value (returns if true)
                    var c = filterExpr.ExecuteScalar(new BsonDocument[] { root }, root, item);

                    if (c.IsBoolean && c.AsBoolean == true)
                    {
                        yield return item;
                    }
                }
            }
        }

        #endregion

        #region Map/Filter/Sort

        public static IEnumerable<BsonValue> MAP(IEnumerable<BsonValue> input, BsonExpression mapExpr, BsonDocument root, BsonDocument parameters)
        {
            // update parameters in expression
            parameters.CopyTo(mapExpr.Parameters);

            foreach (var item in input)
            {
                // execute for each child value and except a first bool value (returns if true)
                var values = mapExpr.Execute(new BsonDocument[] { root }, root, item);

                foreach (var value in values)
                {
                    yield return value;
                }
            }
        }

        public static IEnumerable<BsonValue> FILTER(IEnumerable<BsonValue> input, BsonExpression filterExpr, BsonDocument root, BsonDocument parameters)
        {
            // update parameters in expression
            parameters.CopyTo(filterExpr.Parameters);

            foreach (var item in input)
            {
                // execute for each child value and except a first bool value (returns if true)
                var c = filterExpr.ExecuteScalar(new BsonDocument[] { root }, root, item);

                if (c.IsBoolean && c.AsBoolean == true)
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<BsonValue> SORT(IEnumerable<BsonValue> input, BsonExpression sortExpr, BsonDocument root, BsonDocument parameters)
        {
            //TODO: implement a sort function here

            // update parameters in expression
            parameters.CopyTo(sortExpr.Parameters);

            foreach (var item in input)
            {
                yield return item;
            }
        }

        #endregion

        #region Object Creation

        /// <summary>
        /// Create multi documents based on key-value pairs on parameters. DOCUMENT('_id', 1)
        /// </summary>
        public static BsonValue DOCUMENT_INIT(string[] keys, BsonValue[] values)
        {
            ENSURE(keys.Length == values.Length, "both keys/value must contains same length");

            var doc = new BsonDocument();

            for(var i = 0; i < keys.Length; i++)
            {
                doc[keys[i]] = values[i];
            }

            return doc;
        }

        /// <summary>
        /// Return an array from list of values. Support multiple values but returns a single value
        /// </summary>
        public static BsonValue ARRAY_INIT(BsonValue[] values)
        {
            return new BsonArray(values);
        }

        #endregion
    }
}
