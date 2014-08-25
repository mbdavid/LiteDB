using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    /// <summary>
    /// Class for debug, in "Output Windows" same tests.
    /// </summary>
    [TestClass]
    public class DebugTest
    {
        private const string connectionString = @"C:\Temp\index.db";

        [TestInitialize]
        public void Init()
        {
            File.Delete(connectionString);
        }
    }
}
