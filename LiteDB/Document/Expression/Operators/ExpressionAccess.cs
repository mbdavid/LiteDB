using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    internal class ExpressionAccess
    {
        /// <summary>
        /// Returns value from root document. Returns same document if name are empty
        /// </summary>
        public static IEnumerable<BsonValue> ROOT(BsonValue value, string name)
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
        public static IEnumerable<BsonValue> MEMBER(IEnumerable<BsonValue> values, string name)
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
        public static IEnumerable<BsonValue> ARRAY(IEnumerable<BsonValue> values, int index, BsonExpression expr, BsonDocument root)
        {
            foreach (var value in values)
            {
                if (value.IsArray)
                {
                    var arr = value.AsArray;

                    // [<expr>] - index are an expression
                    if (expr.IsEmpty == false)
                    {
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
    }
}
