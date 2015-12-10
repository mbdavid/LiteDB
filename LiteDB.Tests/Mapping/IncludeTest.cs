using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public class Order
    {
        public ObjectId Id { get; set; }
        public Customer Customer { get; set; }
        public Customer CustomerNull { get; set; }

        public List<Product> Products { get; set; }
        public Product[] ProductArray { get; set; }
        public ICollection<Product> ProductColl { get; set; }
        public List<Product> ProductEmpty { get; set; }
        public List<Product> ProductsNull { get; set; }
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

    public class IncludeDatabase : LiteDatabase
    {
        public IncludeDatabase()
            : base(new MemoryStream())
        {
        }

        public LiteCollection<Customer> Customers { get { return this.GetCollection<Customer>("customers"); } }
        public LiteCollection<Order> Orders { get { return this.GetCollection<Order>("orders"); } }
        public LiteCollection<Product> Products { get { return this.GetCollection<Product>("products"); } }

        protected override void OnModelCreating(BsonMapper mapper)
        {
            mapper.Entity<Order>()
                .DbRef(x => x.Products, "products")
                .DbRef(x => x.ProductArray, "products")
                .DbRef(x => x.ProductColl, "products")
                .DbRef(x => x.ProductEmpty, "products")
                .DbRef(x => x.ProductsNull, "products")
                .DbRef(x => x.Customer, "customers")
                .DbRef(x => x.CustomerNull, "customers");
        }
    }

    [TestClass]
    public class IncludeTest
    {
        [TestMethod]
        public void Include_Test()
        {
            using (var db = new IncludeDatabase())
            {
                var customer = new Customer { Name = "John Doe" };

                var product1 = new Product { Name = "TV", Price = 800 };
                var product2 = new Product { Name = "DVD", Price = 200 };

                // insert ref documents
                db.Customers.Insert(customer);
                db.Products.Insert(new Product[] { product1, product2 });

                var order = new Order
                {
                    Customer = customer,
                    CustomerNull = null,
                    Products = new List<Product>() { product1, product2 },
                    ProductArray = new Product[] { product1 },
                    ProductColl = new List<Product>() { product2 },
                    ProductEmpty = new List<Product>(),
                    ProductsNull = null
                };

                db.Orders.Insert(order);

                var query = db.Orders
                    .Include(x => x.Customer)
                    .Include(x => x.CustomerNull)
                    .Include(x => x.Products)
                    .Include(x => x.ProductArray)
                    .Include(x => x.ProductColl)
                    .Include(x => x.ProductsNull)
                    .FindAll()
                    .FirstOrDefault();

                Assert.AreEqual(customer.Name, query.Customer.Name);
                Assert.AreEqual(product1.Price, query.Products[0].Price);
                Assert.AreEqual(product2.Name, query.Products[1].Name);
                Assert.AreEqual(product1.Name, query.ProductArray[0].Name);
                Assert.AreEqual(product2.Price, query.ProductColl.ElementAt(0).Price);
                Assert.AreEqual(null, query.ProductsNull);
                Assert.AreEqual(0, query.ProductEmpty.Count);
            }
        }
    }
}