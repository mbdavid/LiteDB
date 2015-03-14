using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    public class Order
    {
        public ObjectId Id { get; set; }

        public DbRef<Customer> Customer { get; set; }
    }

    public class Customer
    {
        public ObjectId Id { get; set; }

        public string Name { get; set; }
    }

    [TestClass]
    public class IncludeTest
    {
        [TestMethod]
        public void Include_Test()
        {
            using (var db = new LiteDatabase(DB.Path()))
            {
                var customers = db.GetCollection<Customer>("customers");
                var orders = db.GetCollection<Order>("orders");

                var customer = new Customer
                {
                    Name = "John Doe"
                };

                // insert and set customer.Id
                customers.Insert(customer);

                var order = new Order
                {
                    Customer = new DbRef<Customer>(customers, customer.Id)
                };

                orders.Insert(order);

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
