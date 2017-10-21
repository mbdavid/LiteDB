using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace LiteDB.Tests.Database
{
    #region Model

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

    #endregion

    [TestClass]
    public class DbRef_Index_Tests
    {
        [TestMethod]
        public void DbRef_Index()
        {
            var mapper = new BsonMapper();

            mapper.Entity<DCustomer>()
                .Id(x => x.Login)
                .Field(x => x.Name, "customer_name");

            mapper.Entity<DOrder>()
                .Id(x => x.OrderNumber)
                .Field(x => x.Customer, "cust")
                .DbRef(x => x.Customer, "customers");

            using (var db = new LiteDatabase(new MemoryStream(), mapper))
            {
                var customer = new DCustomer { Login = "jd", Name = "John Doe" };
                var order = new DOrder { Customer = customer };

                var customers = db.GetCollection<DCustomer>("Customers");
                var orders = db.GetCollection<DOrder>("Orders");

                customers.Insert(customer);
                orders.Insert(order);

                // create an index in Customer.Id ref
                // x.Customer.Login == "Customer.$id"
                orders.EnsureIndex(x => x.Customer.Login);

                var query = orders
                    .Include(x => x.Customer)
                    .FindOne(x => x.Customer.Login == "jd");

                Assert.AreEqual(customer.Name, query.Customer.Name);
            }
        }
    }
}