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

namespace LiteDB.Tests.Expressions
{
    [TestClass]
    public class Expressions_Fields_Tests
    {
        [TestMethod]
        public void Expression_Fields()
        {
            void t(BsonExpression expr, params string[] args)
            {
                Assert.AreEqual(expr.Fields.Count, args.Length);

                foreach(var item in expr.Fields.Zip(args, (f, a) => new { Field = f, Expect = a }))
                {
                    Assert.AreEqual(item.Expect, item.Field);
                }
            }

            // simple case
            t("$.Name", "Name");
            t("JustName", "JustName");
            t("$.[\"My First Name\"]", "My First Name");

            // only root field
            t("$.Name.First", "Name");
            t("$.Items[*].Type", "Items");

            // inside new document/array
            t("{ Active, _id }", "Active", "_id");
            t("{ Active, _id: 1 }", "Active");
            t("[ Active, _id, null, UPPER(Name.First)]", "Active", "_id", "Name");

            // no fields
            t("{ Active: 1, _id: 2 }");
            t("123");
            t("UPPER(@p0) = 'JOHN' OR YEAR(NOW()) = 2018");

            // duplicate 
            t("{ Active: active, NewActive: active, Root: $ }", "active", "$");

            // case insensitive (only first field is return)
            t("{ Active: active, NewActive: ACTIVE }", "active");

            // with no root in array
            t("Items[Child = 1].Type = Age", "Items", "Age");

            // with root
            t("Items[$.Root = 1].Type = Age", "Items", "Root", "Age");

            // predicate + method
            t("_id = Age + YEAR(TO_DATETIME(2000, 1, DAY(NewField))) AND UPPER(TRIM(Name)) = @0",
                "_id", "Age", "NewField", "Name");

        }

        [TestMethod]
        public void Expression_Immutable()
        {
            void t(BsonExpression expr, bool isImmutable)
            {
                Assert.AreEqual(isImmutable, expr.IsImmutable);
            }

            // some immutable expression
            t("_id", true);
            t("{ a: 1, n: UPPER(name) }", true);
            t("TO_GUID('00000000-0000-0000-0000-000000000000')", true);

            // using method that are not immutable 
            t("_id + DAY(NOW())", false);
            t("r + 10 > 10 AND GUID() = true", false);
            t("r + 10 > 10 AND Name LIKE OBJECTID() + '%'", false);
            t("_id > @0", false);
        }
    }
}