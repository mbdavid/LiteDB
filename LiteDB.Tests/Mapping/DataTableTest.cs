using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class DataTableTest
    {
        [TestMethod]
        public void DataTable_Test()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                db.Run("db.col1.insert {name:\"John Doe\"}");
                db.Run("db.col1.insert {name:\"Jonatan Doe\", age: 25}");
                db.Run("db.col1.insert {name:\"Maria Doe\", age: 32, active: false}");

                var query = db.GetCollection("col1").FindAll();

                var dt = query.ToDataTable();

                Assert.AreEqual(3, dt.Rows.Count);
                Assert.AreEqual("John Doe", (string)dt.Rows[0]["name"]);
                Assert.AreEqual(25, (int)dt.Rows[1]["age"]);
                Assert.AreEqual(false, (bool)dt.Rows[2]["active"]);
            }
        }
    }

    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Converting IEnumerable of BsonDocument to DataTable (to bind in a GridView)
        /// </summary>
        public static DataTable ToDataTable(this IEnumerable<BsonDocument> docs)
        {
            var dt = new DataTable();

            foreach(var doc in docs)
            {
               var dr = dt.NewRow();

                foreach (var key in doc.Keys)
                {
                    if (!dt.Columns.Contains(key))
                    {
                        dt.Columns.Add(key, doc[key].IsNull ? typeof(string) : doc[key].RawValue.GetType());
                    }

                    dr[key] = doc[key].RawValue;
                }

                dt.Rows.Add(dr);
            }

            return dt;
        }
    }
}