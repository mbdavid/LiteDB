using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Engine
{
    #region Model

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
    }

    // DbRef to cross references
    public class Order
    {
        public ObjectId Id { get; set; }
        public DateTime OrderDate { get; set; }
        public Customer Customer { get; set; }
        public List<Product> Products { get; set; }
    }

    #endregion

    [TestClass]
    public class Site_Examples_Tests
    {
        [TestMethod]
        public void DbRef_For_Cross_References()
        {
            // Re-use mapper from global instance
            var mapper = BsonMapper.Global;

            // Produts and Customer are other collections (not embedded document)
            // you can use [BsonRef("colname")] attribute
            mapper.Entity<Order>()
                .DbRef(x => x.Products, "products")
                .DbRef(x => x.Customer, "customers");

            using (var file = new TempFile())
            using (var db = new LiteDatabase(file.Filename))
            {
                var customers = db.GetCollection<Customer>("customers");
                var products = db.GetCollection<Product>("products");
                var orders = db.GetCollection<Order>("orders");

                // create examples
                var john = new Customer { Name = "John Doe" };
                var tv = new Product { Description = "TV Sony 44\"", Price = 799 };
                var iphone = new Product { Description = "iPhone X", Price = 999 };
                var order1 = new Order { OrderDate = new DateTime(2017, 1, 1), Customer = john, Products = new List<Product>() { iphone, tv } };
                var order2 = new Order { OrderDate = new DateTime(2017, 10, 1), Customer = john, Products = new List<Product>() { iphone } };

                // insert into collections
                customers.Insert(john);
                products.Insert(new Product[] { tv, iphone });
                orders.Insert(new Order[] { order1, order2 });

                // create index in OrderDate
                orders.EnsureIndex(x => x.OrderDate);

                // When query Order, includes references
                var query = orders
                    .Include(x => x.Customer)
                    .Include(x => x.Products)
                    .Find(x => x.OrderDate == new DateTime(2017, 1, 1));

                // Each instance of Order will load Customer/Products references
                foreach(var c in query)
                {
                    Console.WriteLine("#{0} - {1}", c.Id, c.Customer.Name);

                    foreach(var p in c.Products)
                    {
                        Console.WriteLine(" > {0} - {1:c}", p.Description, p.Price);
                    }
                }
            }
        }
    }
}