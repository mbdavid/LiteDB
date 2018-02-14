using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    internal class BsonExpressionOperators
    {
        /// <summary>
        /// Add two number values. If any side are string, concat left+right as string. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> ADD(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                // if any side are string, concat
                if (value.First.IsString || value.Second.IsString)
                {
                    yield return value.First.RawValue?.ToString() + value.Second.RawValue?.ToString();
                }
                else if (value.First.IsDateTime && value.Second.IsNumber)
                {
                    yield return value.First.AsDateTime.AddDays(value.Second.AsDouble);
                }
                else if (value.First.IsNumber && value.Second.IsDateTime)
                {
                    yield return value.Second.AsDateTime.AddDays(value.First.AsDouble);
                }
                else if (!value.First.IsNumber || !value.Second.IsNumber)
                {
                    continue;
                }
                else
                {
                    yield return value.First + value.Second;
                }
            }
        }

        /// <summary>
        /// Minus two number values. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> MINUS(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (value.First.IsDateTime && value.Second.IsNumber)
                {
                    yield return value.First.AsDateTime.AddDays(-value.Second.AsDouble);
                }
                else if (value.First.IsNumber && value.Second.IsDateTime)
                {
                    yield return value.Second.AsDateTime.AddDays(-value.First.AsDouble);
                }
                else if (!value.First.IsNumber || !value.Second.IsNumber)
                {
                    continue;
                }
                else
                {
                    yield return value.First - value.Second;
                }
            }
        }

        /// <summary>
        /// Multiply two number values. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> MULTIPLY(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.First.IsNumber || !value.Second.IsNumber) continue;

                yield return value.First * value.Second;
            }
        }

        /// <summary>
        /// Divide two number values. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> DIVIDE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.First.IsNumber || !value.Second.IsNumber) continue;

                yield return value.First / value.Second;
            }
        }

        /// <summary>
        /// Mod two number values. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> MOD(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.First.IsNumber || !value.Second.IsNumber) continue;

                yield return value.First % value.Second;
            }
        }

        /// <summary>
        /// Test if left and right are same value. Returns true or false. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> EQ(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First == value.Second;
            }
        }

        /// <summary>
        /// Test if left and right are not same value. Returns true or false. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> NEQ(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First != value.Second;
            }
        }

        /// <summary>
        /// Test if left is greater than right value. Returns true or false. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> GT(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First > value.Second;
            }
        }

        /// <summary>
        /// Test if left is greater or equals than right value. Returns true or false. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> GTE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First >= value.Second;
            }
        }

        /// <summary>
        /// Test if left is less than right value. Returns true or false. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> LT(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First < value.Second;
            }
        }

        /// <summary>
        /// Test if left is less or equals than right value. Returns true or false. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> LTE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First <= value.Second;
            }
        }

        /// <summary>
        /// Test left AND right value. Returns true or false. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> AND(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First && value.Second;
            }
        }

        /// <summary>
        /// Test left OR right value. Returns true or false. Support multiples values
        /// </summary>
        public static IEnumerable<BsonValue> OR(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First || value.Second;
            }
        }

        /// <summary>
        /// Returns value from root document. Returns same document if name are empty
        /// </summary>
        public static IEnumerable<BsonValue> ROOT_PATH(BsonValue value, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                yield return value;
            }
            else if (value.IsDocument)
            {
                if (value.AsDocument.TryGetValue(name, out BsonValue item))
                {
                    // fill destroy action to remove value from root
                    item.Destroy = () => value.AsDocument.Remove(name);

                    yield return item;
                }
            }
        }

        /// <summary>
        /// Return a value from a value as document. If has no name, just return values ($). If value are not a document, do not return anything
        /// </summary>
        public static IEnumerable<BsonValue> MEMBER_PATH(IEnumerable<BsonValue> values, string name)
        {
            foreach (var doc in values.Where(x => x.IsDocument).Select(x => x.AsDocument))
            {
                if (doc.TryGetValue(name, out BsonValue item))
                {
                    // fill destroy action to remove value from parent document
                    item.Destroy = () => doc.Remove(name);

                    yield return item;
                }
            }
        }

        /// <summary>
        /// Returns all values from array according index. If index are MaxValue, return all values
        /// </summary>
        public static IEnumerable<BsonValue> ARRAY_PATH(IEnumerable<BsonValue> values, int index, BsonExpression expr, BsonDocument root, BsonDocument parameters)
        {
            foreach (var value in values)
            {
                if (value.IsArray)
                {
                    var arr = value.AsArray;

                    // [<expr>] - index are an expression
                    if (expr.Type != BsonExpressionType.Empty)
                    {
                        // update parameters in expression
                        parameters.CopyTo(expr.Parameters);

                        foreach (var item in arr)
                        {
                            // execute for each child value and except a first bool value (returns if true)
                            var c = expr.Execute(root, item, true).First();

                            if (c.IsBoolean && c.AsBoolean == true)
                            {
                                // fill destroy action to remove value from parent array
                                item.Destroy = () => arr.Remove(item);

                                yield return item;
                            }
                        }
                    }
                    // [*] - index are all values
                    else if (index == int.MaxValue)
                    {
                        foreach (var item in arr)
                        {
                            // fill destroy action to remove value from parent array
                            item.Destroy = () => arr.Remove(item);

                            yield return item;
                        }
                    }
                    // [n] - fixed index
                    else
                    {
                        var idx = index < 0 ? arr.Count + index : index;

                        if (arr.Count > idx)
                        {
                            var item = arr[idx];

                            // fill destroy action to remove value from parent array
                            item.Destroy = () => arr.Remove(item);

                            yield return item;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create a single document based on key-value pairs on parameters. DOCUMENT('_id', 1)
        /// </summary>
        public static IEnumerable<BsonValue> DOCUMENT_INIT(IEnumerable<BsonValue> keys, IEnumerable<IEnumerable<BsonValue>> values)
        {
            var doc = new BsonDocument();

            foreach (var pair in keys.ZipValues(values.Select(x => x.FirstOrDefault())))
            {
                var key = pair.First;
                var value = pair.Second;

                if (value != null)
                {
                    doc[key.AsString] = value;
                }
            }

            yield return doc;
        }

        /// <summary>
        /// Return an array from list of values. Support multiple values but returns a single value
        /// </summary>
        public static IEnumerable<BsonValue> ARRAY_INIT(IEnumerable<IEnumerable<BsonValue>> values)
        {
            yield return new BsonArray(values.SelectMany(x => x));
        }
    }
}
