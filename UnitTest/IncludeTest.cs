using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    public class CustomerInclude
    {
        public ObjectId Id { get; set; }

        [BsonIndex(true)]
        public string Name { get; set; }

        public DateTime CreationDate { get; set; }
    }

    public class OrderInclude
    {
        [BsonId]
        public int OrderKey { get; set; }

        public DateTime Date { get; set; }

        public DbRef<CustomerInclude> Customer { get; set; }
    }

    [TestClass]
    public class IncludeTest
    {
        [TestMethod]
        public void Include_Test()
        {
            using (var db = new LiteDatabase(DB.Path()))
            {
                var customers = db.GetCollection<CustomerInclude>("customers");
                var orders = db.GetCollection<OrderInclude>("orders");

                var customer = new CustomerInclude
                {
                    Id = ObjectId.NewObjectId(),
                    Name = "Mauricio"
                };

                var order = new OrderInclude
                {
                    Date = DateTime.Now,
                    Customer = new DbRef<CustomerInclude>(customers, customer.Id)
                };

                db.BeginTrans();

                customers.Insert(customer);
                orders.Insert(order);

                db.Commit();

                var query = orders
                    .Include((x) => x.Customer.Fetch(db))
                    .FindAll()
                    .Select(x => new { CustomerName = x.Customer.Item.Name })
                    .FirstOrDefault();

                Assert.AreEqual(customer.Name, query.CustomerName);

            }
        }
    }
}
