using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public class Util
    {
        [DebuggerHidden]
        public static void Compare<T>(T[] first, T[] second, bool sort = false)
            where T : IEqualityComparer<T>
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
                var r = zip.First.Equals(zip.First, zip.Second);

                if (r == false)
                {
                    Assert.Fail($"Values are not same `{zip.First}` and `{zip.Second}`");
                }
            }
        }
    }
}