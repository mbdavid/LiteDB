using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    [TestClass]
    public class IncludeTest
    {
        [TestMethod]
        public void Include_Test()
        {
            using (var db = new LiteDatabase(DB.Path()))
            {
                var customers = db.GetCollection<Customer>("Customer");
                var orders = db.GetCollection<Order>("Order");

                var customer1 = new Customer { CustomerId = Guid.NewGuid(), Name = "Mauricio" };
                var order1 = new Order { OrderKey = 1, Date = DateTime.Now, Customer = new LiteDBRef<Customer>(customers, customer1.CustomerId) };
                var order2 = new Order { OrderKey = 2, Date = new DateTime(2000, 1, 1) /*, Customer = new LiteDBRef<Customer>(customers, customer1.CustomerId)*/ };

                customers.EnsureIndex(x => x.Name, true);

                customers.Insert(customer1);
                orders.Insert(order1);
                orders.Insert(order2);

                var query = orders
                    //.Include((x) => x.Customer.Fetch(db))
                    .FindAll()
                    //.Select(x => new { x.OrderKey, Cust = x.Customer.Item.Name, CustomerInstance = x.Customer })
                    .FirstOrDefault();

                //Assert.AreEqual(customer1.Name, query.Cust);

            }
        }
    }
}
