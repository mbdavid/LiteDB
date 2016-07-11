using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LiteDB.Tests
{
    public class Patient
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public Ward Ward { get; set; }
    }

    public class Ward
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Hospital : LiteDatabase
    {
        public LiteCollection<Patient> Patients { get { return this.GetCollection<Patient>("Patient"); } }

        public LiteCollection<Ward> Wards { get { return this.GetCollection<Ward>("Ward"); } }
    }

    [TestClass]
    public class IndexDbRefTest : TestBase
    {
        [TestMethod]
        public void IndexDbRef_Order()
        {
           var dbFile = DB.RandomFile();

           var bsonMapper = new BsonMapper();

         bsonMapper.Entity<Patient>()
       //.Index(x => x.Ward.Id)
       //.Index("Ward.Id")
       .DbRef(x => x.Ward, "Ward");

         using (var db = LiteDatabaseFactory.Create<Hospital>(dbFile, bsonMapper))
            {
                var w1 = new Ward { Id = 1, Name = "Ward 1" };
                var w2 = new Ward { Id = 2, Name = "Ward 2" };

                var p1 = new Patient { Id = 1, Name = "John", Ward = w1 };
                var p2 = new Patient { Id = 2, Name = "Dooe", Ward = w1 };
                var p3 = new Patient { Id = 3, Name = "Jona", Ward = w2 };

                db.Patients.EnsureIndex(x => x.Ward.Id);

                db.Wards.Insert(new[] { w1, w2 });
                db.Patients.Insert(new[] { p1, p2, p3 });

                var query = db.Patients
                    .Include(x => x.Ward)
                    .Find(x => x.Ward.Id == 1)
                    //.Find(Query.EQ("Ward.$id", 1))
                    .ToArray();

                Assert.AreEqual(2, query.Count());
            }

         TestPlatform.DeleteFile(dbFile);

      }
   }
}