using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB.Tests
{
    [TestClass]
    public class LinqVisitorTest
    {
        [TestMethod]
        public void LinqVisitor_Test()
        {
            var mapper = new BsonMapper();
            mapper.ResolveFieldName = (x) => x.ToLower();

            var qv = new QueryVisitor<User>(mapper);

            //var q1 = qv.Visit(x => x.Id == 1); // Query.EQ("_id", 1)
            
            //var q2 = qv.Visit(x => x.Active); // Query.EQ("Active", true);

            // not
            //var q3 = qv.Visit(x => !x.Active); // Query.Not("Active", true)
            //var q4 = qv.Visit(x => !(x.Id == 1)); // Query.Not(Query.EQ("_id", 1))
            //var q5 = qv.Visit(x => !x.Name.StartsWith("john")); // Query.Not(Query.StartsWith("John"))

            // enum
            //var q4 = qv.Visit(x => x.OS == PlatformID.Unix);

            //var q6 = qv.Visit(x => new int[] { 1, 2 }.Contains(x.Id));

            //var q7 = qv.Visit(x => x.Names.Contains("John"));



            //**var q8 = qv.Visit(x => x.Domains.Any(d => d.DomainName == "ABC"));

            //****var q8 = qv.Visit(x => x.Names.Any(z => z.StartsWith("John")));
            // => Query.StartsWith("Names", "John")

        }
    }
}