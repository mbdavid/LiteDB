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

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Sql_Tests
    {
        #region Run Tests

        public BsonDocument Run(string name, Action<LiteEngine> setup = null)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"LiteDB.Tests.Engine.SqlTests.{name}.sql");
            var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            var par = new BsonDocument();

            using (var db = new LiteEngine())
            {
                setup?.Invoke(db);

                try
                {
                    using (var data = db.Execute(sql, par))
                    {
                        while (data.NextResult()) ;
                    }
                }
                catch(LiteException ex) when (ex.ErrorCode == LiteException.UNEXPECTED_TOKEN)
                {
                    var p = (int)ex.Position;
                    var start = (int)Math.Max(p - 30, 1) - 1;
                    var end = Math.Min(p + 15, sql.Length);
                    var length = end - start;
                    var str = sql.Substring(start, length).Replace('\n', ' ').Replace('\r', ' ');

                    Assert.Fail($"{ex.Message} - {str}");
                }
            }

            return par;
        }

        #endregion

        [TestMethod]
        public void Sql_Insert_Commands()
        {
            var output = this.Run("Insert");

            Assert.AreEqual(1, output["insert1"].AsInt32, "Insert into single document");
            Assert.AreEqual(3, output["insert3"].AsInt32, "Insert into multi document");

            Assert.AreEqual(1, output["int"].AsInt32, "AutoId using Int32");
            Assert.AreEqual(1, output["long"].AsInt32, "AutoId using Long");
            Assert.AreEqual(1, output["date"].AsInt32, "AutoId using Date");
            Assert.AreEqual(1, output["guid"].AsInt32, "AutoId using Guid");
            Assert.AreEqual(1, output["objectid"].AsInt32, "AutoId using ObjectId");
        }

        [TestMethod]
        public void Sql_Update_Commands()
        {
            var output = this.Run("Update", db => db.Insert("person", DataGen.Person(1, 10)));

            Assert.AreEqual(1, output["count1"].AsInt32, "Update single document");
            Assert.AreEqual(99, output["newAge"].AsInt32, "Document was updated");
        }

        [TestMethod]
        public void Sql_Query_Commands()
        {
            var output = this.Run("Query", db => db.Insert("person", DataGen.Person(1, 1000)));

            Assert.AreEqual("Brendan Maxwell", output["name100"].AsString);
            Assert.AreEqual("Brendan Maxwell", output["nameAll"][100].AsString);

            Assert.AreEqual(5, output["groupBy1"].AsArray.Count);
        }
    }
}