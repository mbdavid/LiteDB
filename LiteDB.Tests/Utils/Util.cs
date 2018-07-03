using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public class Util
    {
        public static void Compare(BsonValue[] first, BsonValue[] second, bool sort = false)
        {
            if (first.Length != second.Length)
            {
                Assert.Fail("Arrays with diferent item count");
            }

            if (sort)
            {
                first = first.OrderBy(x => x).ToArray();
                second = second.OrderBy(x => x).ToArray();
            }

            foreach(var zip in first.Zip(second, (First, Second) => new { First,  Second }))
            {
                var r = zip.First.CompareTo(zip.Second);

                if (r != 0)
                {
                    Assert.Fail($"Values are not same `{zip.First}` and `{zip.Second}`");
                }
            }
        }
    }
}