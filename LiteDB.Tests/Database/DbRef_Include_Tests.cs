using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Database
{
    #region Model 

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
        public Address MainAddress { get; set; }
    }

    public class Address
    {
        public int Id { get; set; }
        public string StreetName { get; set; }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public Address SupplierAddress { get; set; }
    }

    #endregion

    [TestClass]
    public class DbRef_Include_Tests
    {
        [TestMethod]
        public void DbRef_Include()
        {
            var mapper = new BsonMapper();

            mapper.Entity<Order>()
               .DbRef(x => x.Products, "products")
               .DbRef(x => x.ProductArray, "products")
               .DbRef(x => x.ProductColl, "products")
               .DbRef(x => x.ProductEmpty, "products")
               .DbRef(x => x.ProductsNull, "products")
               .DbRef(x => x.Customer, "customers")
               .DbRef(x => x.CustomerNull, "customers");

            mapper.Entity<Customer>()
                .DbRef(x => x.MainAddress, "addresses");

            mapper.Entity<Product>()
                .DbRef(x => x.SupplierAddress, "addresses");

            using (var db = new LiteDatabase(new MemoryStream(), mapper))
            {
                var address = new Address { StreetName = "3600 S Las Vegas Blvd" };
                var customer = new Customer { Name = "John Doe", MainAddress = address };

                var product1 = new Product { Name = "TV", Price = 800, SupplierAddress = address };
                var product2 = new Product { Name = "DVD", Price = 200 };

                var customers = db.GetCollection<Customer>("customers");
                var addresses = db.GetCollection<Address>("addresses");
                var products = db.GetCollection<Product>("products");
                var orders = db.GetCollection<Order>("orders");

                // insert ref documents
                addresses.Insert(address);
                customers.Insert(customer);
                products.Insert(new Product[] { product1, product2 });

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

                mapper.SerializeNullValues = true;

                var dOrder = mapper.ToDocument<Order>(order);


                orders.Insert(order);

                //var r0 = orders
                //    .Include(x => x.Products)
                //    .Include(x => x.Products[0].SupplierAddress)
                //    .FindAll()
                //    .FirstOrDefault();

                orders.EnsureIndex(x => x.Customer.Id);

                // query orders using customer $id
                // include customer data and customer address
                var r1 = orders
                    .Include(x => x.Customer)
                    .Include(x => x.Customer.MainAddress)
                    .Find(x => x.Customer.Id == 1)
                    .FirstOrDefault();

                Assert.AreEqual(order.Id, r1.Id);
                Assert.AreEqual(order.Customer.Name, r1.Customer.Name);
                Assert.AreEqual(order.Customer.MainAddress.StreetName, r1.Customer.MainAddress.StreetName);

                // include all 
                var result = orders
                    .Include(x => x.Customer)
                    .Include("Customer.MainAddress")
                    .Include(x => x.CustomerNull)
                    .Include(x => x.Products)
                    .Include(x => x.ProductArray)
                    .Include(x => x.ProductColl)
                    .Include(x => x.ProductsNull)
                    // not supported yet
                    .Include(x => x.Products[0].SupplierAddress)
                    .FindAll()
                    .FirstOrDefault();

                Assert.AreEqual(customer.Name, result.Customer.Name);
                Assert.AreEqual(customer.MainAddress.StreetName, result.Customer.MainAddress.StreetName);
                Assert.AreEqual(product1.Price, result.Products[0].Price);
                Assert.AreEqual(product2.Name, result.Products[1].Name);
                Assert.AreEqual(product1.Name, result.ProductArray[0].Name);
                Assert.AreEqual(product2.Price, result.ProductColl.ElementAt(0).Price);
                Assert.AreEqual(null, result.ProductsNull);
                Assert.AreEqual(0, result.ProductEmpty.Count);

                // now, delete reference 1x1 and 1xN
                customers.Delete(customer.Id);

                products.Delete(product1.ProductId);

                var json = db.Engine.Find("orders", Query.All(), new string[] { "$.Customer", "$.Products[*]" })
                    .FirstOrDefault()
                    .ToString();

                var result2 = orders
                    .Include(x => x.Customer)
                    .Include(x => x.Products)
                    .FindAll()
                    .FirstOrDefault();

                // must missing customer and has only 1 product

                Assert.IsNull(result2.Customer);
                Assert.AreEqual(1, result2.Products.Count);

                // property ProductArray contains only deleted "product1", but has no include on query, so must returns deleted

                Assert.AreEqual(1, result2.ProductArray.Length);


            }
        }

        [TestMethod] 
        public void DbRef_IncludeAll()
        {
            // Setup the mapper
            var mapper = new BsonMapper()
            {
                SerializeNullValues = true
            };

            // Specify the reference fields
            mapper.Entity<Order>()
                .DbRef(x => x.Products, "products")
                .DbRef(x => x.ProductArray, "products")
                .DbRef(x => x.ProductColl, "products")
                .DbRef(x => x.ProductEmpty, "products")
                .DbRef(x => x.ProductsNull, "products")
                .DbRef(x => x.Customer, "customers")
                .DbRef(x => x.CustomerNull, "customers");

            mapper.Entity<Customer>()
                .DbRef(x => x.MainAddress, "addresses");

            mapper.Entity<Product>()
                .DbRef(x => x.SupplierAddress, "addresses");

            using (var db = new LiteDatabase(new MemoryStream(), mapper))
            {
                // setup some sample data
                var address = new Address { StreetName = "3600 S Las Vegas Blvd" };
                var customer = new Customer { Name = "John Doe", MainAddress = address };

                var product1 = new Product { Name = "TV", Price = 800, SupplierAddress = address };
                var product2 = new Product { Name = "DVD", Price = 200 };

                var customers = db.GetCollection<Customer>("customers");
                var addresses = db.GetCollection<Address>("addresses");
                var products = db.GetCollection<Product>("products");

                // insert ref documents
                addresses.Insert(address);
                customers.Insert(customer);
                products.Insert(new Product[] { product1, product2 });

                // Insert order
                var orders = db.GetCollection<Order>("orders");
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

                orders.Insert(order);
                orders.EnsureIndex(x => x.Customer.Id);

                // query orders using customer $id
                // include all references to its max depth.
                var r1 = orders
                    .IncludeAll()
                    .Find(x => x.Customer.Id == 1)
                    .FirstOrDefault();

                Assert.AreEqual(order.Id, r1.Id);
                Assert.AreEqual(customer.Name, r1.Customer.Name);
                Assert.AreEqual(customer.MainAddress.StreetName, r1.Customer.MainAddress.StreetName);
                Assert.AreEqual(product1.Price, r1.Products[0].Price);
                Assert.AreEqual(product2.Name, r1.Products[1].Name);
                Assert.AreEqual(product1.Name, r1.ProductArray[0].Name);
                Assert.AreEqual(product2.Price, r1.ProductColl.ElementAt(0).Price);

                // Just get one record, (will be the same as before)
                // But only get the references for one level deep.
                var result = orders
                    .IncludeAll(1)
                    .FindAll()
                    .FirstOrDefault();

                Assert.AreEqual(customer.Name, result.Customer.Name);
                Assert.AreEqual(null, result.Customer.MainAddress.StreetName);
                Assert.AreEqual(null, result.Products[0].SupplierAddress.StreetName);
                Assert.AreEqual(product2.Name, result.Products[1].Name);
                Assert.AreEqual(product1.Name, result.ProductArray[0].Name);
                Assert.AreEqual(product2.Price, result.ProductColl.ElementAt(0).Price);
                Assert.AreEqual(null, result.ProductsNull);
                Assert.AreEqual(0, result.ProductEmpty.Count);
            }
        }
    }
}