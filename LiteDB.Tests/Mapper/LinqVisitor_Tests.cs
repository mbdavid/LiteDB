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
using System.Linq.Expressions;

namespace LiteDB.Tests.Mapper
{
    [TestClass]
    public class LinqVisitor_Tests
    {
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime Age { get; set; }
            public bool Active { get; set; }
        }

        [TestMethod]
        public void Simple_Linq_Visitor()
        {
            Assert.AreEqual("$.Name", this.Get(x => x.Active));


        }

        private string Get<K>(Expression<Func<User, K>> expr)
        {
            var mapper = new BsonMapper();
            var visitor = new QueryVisitor<User>(mapper);

            var expression = visitor.VisitExpression(expr);

            return expression.Source;
        }
    }
}