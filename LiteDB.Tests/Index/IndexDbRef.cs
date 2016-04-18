using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
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
        public Hospital()
            : base(@"c:\temp\dbf.db") // using a memory database just for testing
        {
        }

        public LiteCollection<Patient> Patients { get { return this.GetCollection<Patient>("Patient"); } }

        public LiteCollection<Ward> Wards { get { return this.GetCollection<Ward>("Ward"); } }

        protected override void OnModelCreating(BsonMapper mapper)
        {
            mapper.Entity<Patient>()
                //.Index(x => x.Ward.Id)
                //.Index("Ward.Id")
                .DbRef(x => x.Ward, "Ward");
        }
    }

    [TestClass]
    public class IndexDbRefTest
    {
        [TestMethod]
        public void IndexDbRef_Order()
        {
            File.Delete(@"c:\temp\dbf.db");

            using (var db = new Hospital())
            {
                var w1 = new Ward { Id = 1, Name = "Ward 1" };
                var w2 = new Ward { Id = 2, Name = "Ward 2" };

                var p1 = new Patient { Id = 1, Name = "John", Ward = w1 };
                var p2 = new Patient { Id = 2, Name = "Dooe", Ward = w1 };
                var p3 = new Patient { Id = 3, Name = "Jona", Ward = w2 };

                db.Patients.EnsureIndex(x => x.Ward.Id);

                db.Wards.Insert(new Ward[] { w1, w2 });
                db.Patients.Insert(new Patient[] { p1, p2, p3 });

                var query = db.Patients
                    .Include(x => x.Ward)
                    .Find(x => x.Ward.Id == 1)
                    //.Find(Query.EQ("Ward.$id", 1))
                    .ToArray();

                Assert.AreEqual(2, query.Count());
            }
        }
    }
}