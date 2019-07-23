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

namespace LiteDB.Tests.Query
{
    public class Person_Tests
    {
        protected readonly Person[] local;

        protected LiteDatabase db;
        protected LiteCollection<Person> collection;

        public Person_Tests()
        {
            this.local = DataGen.Person().ToArray();
        }

        [TestInitialize]
        public void Init()
        {
            db = new LiteDatabase(":memory:");
            collection = db.GetCollection<Person>("person");
            collection.Insert(this.local);
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }
    }
}