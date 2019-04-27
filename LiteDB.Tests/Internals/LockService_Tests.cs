using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;
using System.Threading;
using System.Diagnostics;

namespace LiteDB.Internals
{
    [TestClass]
    public class LockServices_Tests
    {
        [TestMethod]
        public void Collection_Lock()
        {
            var locker = new LockService(TimeSpan.FromSeconds(10), false);

            var ta = new Task(async () =>
            {

            });

            ta.Start();

            Task.WaitAll(ta);

        }
    }
}