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
                db.Run("db.col1.insert {name:\"John Doe\", age: 38, active: true}");
                db.Run("db.col1.insert {name:\"Jonatan Doe\", age: 25, active: true}");
                db.Run("db.col1.insert {name:\"Maria Doe\", age: 32, active: false}");

                var query = db.GetCollection("col1").FindAll();

                var dt = query.ToDataTable();

                Assert.AreEqual(3, dt.Rows.Count);
                Assert.AreEqual("John Doe", (string)dt.Rows[0]["name"]);
                Assert.AreEqual(38, (int)dt.Rows[0]["age"]);
                Assert.AreEqual(true, (bool)dt.Rows[0]["active"]);
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
                if(dt.Columns.Count == 0)
                {
                    foreach(var key in doc.Keys)
                    {
                        dt.Columns.Add(key, doc[key].IsNull ? typeof(string) : doc[key].RawValue.GetType());
                    }
                }

                var dr = dt.NewRow();

                foreach (var key in doc.Keys)
                {
                    dr[key] = doc[key].RawValue;
                }

                dt.Rows.Add(dr);
            }

            return dt;
        }
    }
}