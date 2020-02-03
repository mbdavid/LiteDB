using System;
using System.IO;
using System.Linq;
using LiteDB;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Database
{
    public class Update_V5_Tests
    {
        public class User
        {
            public LiteDB.ObjectId Id { get; set; }
            public string Name { get; set; }
            public double Age { get; set; }
        }

        [Fact]
        public void Insert_User_Test()
        {
            using(var f = new TempFile())
            {
                this.Execute(f.Filename);
                this.Execute(f.Filename);
            }
        }

        private void Execute(string filename)
        {
            using (var db = new LiteDB.LiteDatabase(filename))
            {
                var cols = db.GetCollection<User>();

                if (cols.Count() == 0)
                {
                    cols.Insert(new User() { Name = "Ivan" });
                }

                var user1 = cols.FindOne(x => x.Name == "Ivan");

                Console.WriteLine($"User1.Age: {user1.Age}");
                user1.Age++;
                var upd = cols.Update(user1);

                var user2 = cols.FindOne(x => x.Name == "Ivan");

                Console.WriteLine($"User2.Age: {user2.Age}");
                Console.WriteLine($"-----");

                /*if (db.GetCollection("User").Count() == 0)
                {
                    var a = db.Execute("insert into User values {Name: 'Ivan', Age: 0.0}");
                }
                var b = db.Execute("select $ from User where Name = 'Ivan'");
                var c = db.Execute("update User set Age = Age + 1 where $.Name = 'Ivan'");
                var d = db.Execute("select $ from User where Name = 'Ivan'");*/
            }

        }
    }
}