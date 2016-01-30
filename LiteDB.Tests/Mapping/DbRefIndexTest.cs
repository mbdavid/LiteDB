using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public class DCustomer
    {
        public string Login { get; set; }
        public string Name { get; set; }
    }

    public class DOrder
    {
        public int OrderNumber { get; set; }
        public DCustomer Customer { get; set; }
    }

    public class DbRefIndexDatabase : LiteDatabase
    {
        public DbRefIndexDatabase(string f)
            : base(f)
        {
        }

        public LiteCollection<DCustomer> Customers { get { return this.GetCollection<DCustomer>("customers"); } }
        public LiteCollection<DOrder> Orders { get { return this.GetCollection<DOrder>("orders"); } }

        protected override void OnModelCreating(BsonMapper mapper)
        {
            mapper.Entity<DCustomer>()
                .Id(x => x.Login)
                .Field(x => x.Name, "customer_name");

            mapper.Entity<DOrder>()
                .Id(x => x.OrderNumber)
                .Field(x => x.Customer, "cust")
                .DbRef(x => x.Customer, "customers");
        }
    }

    [TestClass]
    public class DbRefIndexTest
    {
        [TestMethod]
        public void DbRefIndexe_Test()
        {
            var f = "";
            f = @"C:\Git\LiteDB\LiteDB.Shell\bin\Debug\_dbref.db";
            File.Delete(f);

            using (var db = new DbRefIndexDatabase(f))
            {
                var customer = new DCustomer { Login = "jd", Name = "John Doe" };
                var order = new DOrder { OrderNumber = 1, Customer = customer };

                db.Customers.Insert(customer);
                db.Orders.Insert(order);

                // create an index in Customer.Id ref
                db.Orders.EnsureIndex(x => x.Customer.Login);

                var query = db.Orders
                    .Include(x => x.Customer)
                    .FindOne(x => x.Customer.Login == "jd");

                Assert.AreEqual(customer.Name, query.Customer.Name);
            }
        }
    }
}