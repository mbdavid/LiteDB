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
            using (var f = new TempFile())
            {
                this.Execute(f.Filename, 0);
                this.Execute(f.Filename, 1);
            }
        }

        private void Execute(string filename, int pass)
        {
            using (var db = new LiteDB.LiteDatabase(filename))
            {
                var cols = db.GetCollection<User>();

                if (cols.Count() == 0)
                {
                    cols.Insert(new User() { Name = "Ivan" });
                }

                var user1 = cols.FindOne(x => x.Name == "Ivan");
                var age1 = user1.Age.Should().Be(0 + pass);

                user1.Age++;
                var upd = cols.Update(user1);

                var user2 = cols.FindOne(x => x.Name == "Ivan");
                var age2 = user2.Age.Should().Be(1 + pass);
            }

        }
    }
}