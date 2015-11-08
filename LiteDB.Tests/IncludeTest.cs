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
        public Customer Customer { get; set; }
        public List<Product> Products { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
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
                var products = db.GetCollection<Product>("products");
                var orders = db.GetCollection<Order>("orders");

                db.Mapper.Entity<Order>()
                    //.DbRef(x => x.Products, "products")
                    .DbRef(x => x.Customer, "customers");

                var customer = new Customer { Name = "John Doe" };

                var product1 = new Product { Name = "TV", Price = 800 };
                var product2 = new Product { Name = "DVD", Price = 200 };

                // insert ref documents
                customers.Insert(customer);
                products.Insert(new Product[] { product1, product2 });

                var order = new Order
                {
                    Customer = customer,
                    Products = new List<Product>() { product1, product2 }
                };

                var orderJson = JsonSerializer.Serialize(db.Mapper.ToDocument(order), true);

                var nOrder = db.Mapper.Deserialize<Order>(JsonSerializer.Deserialize(orderJson));

                orders.Insert(order);

                var query = orders
                    //.Include((x) => x.Customer.Fetch(db))
                    .FindAll()
                    .FirstOrDefault();


            }
        }
    }
}
