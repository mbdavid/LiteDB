using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiteDB.Tests
{
    [TestClass]
    public class Initialize
    {
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            // wait all threads close FileDB
            System.Threading.Thread.Sleep(2000);

            DB.DeleteFiles();
        }
    }
}