using System;
using System.Linq;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
	[TestClass]
	public class ForeachTest
	{
		[TestMethod]
		public void Foreach()
		{
			const string path = @"C:\5\test.db";
      const string tableName = "users";
			const int n = 100;

			using (var db = new LiteDatabase(path))
			{
				db.BeginTrans();
				db.DropCollection(tableName);
				var users = db.GetCollection(tableName);
				for (int i = 0; i < n; i++)
				{
					var doc = new BsonDocument();
					doc["_id"] = i;
					users.Insert(doc);
				}
				db.Commit();
			}

			using (var db = new LiteDatabase(path))
			{
				var users = db.GetCollection(tableName);
				var items = Enumerable.Range(0, n).ToList();
				foreach (var user in users)
				{
					var id = user["_id"].AsInt32;
					items.Remove(id);
				}
				Assert.AreEqual(items.Count,0);
			}
		}
	}
}
