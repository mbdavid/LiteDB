using System;
using System.Linq;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
	[TestClass]
	public class CommitTest
	{
		[TestMethod]
		public void ActualizationAfterCommitTest()
		{
			const string tableName = "users";
			const int n = 100;

			using (var db = new LiteDatabase(@"C:\5\test.db"))
			using (var db2 = new LiteDatabase(@"C:\5\test.db"))
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

				users = db2.GetCollection(tableName);
				Assert.AreEqual(users.FindAll().Count(),n);
			}
    }
	}
}
