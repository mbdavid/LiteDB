using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace LiteDB.Tests.Repository
{
    #region Model

    public class ROrder
    {
        [BsonId]
        public int OrderNumber { get; set; }
        public DateTime Date { get; set; }
        public bool Active { get; set; }

        [BsonRef]
        public RCustomer Customer { get; set; }

        [BsonRef]
        public List<RProduct> Products { get; set; }

        public ROrder()
        {
            Products = new List<RProduct>();
        }
    }

    public class RCustomer
    {
        [BsonId]
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
    }

    public class RProduct
    {
        [BsonId]
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
    }

    #endregion

    [TestClass]
    public class Repository_Tests
    {
        [TestMethod]
        public void Simple_Repository()
        {
            using (var f = new TempFile())
            using (var db = new LiteRepository(f.Filename))
            {
                var p1 = new RProduct { ProductName = "Table", Price = 100 };
                var p2 = new RProduct { ProductName = "Chair", Price = 40 };
                var p3 = new RProduct { ProductName = "Couch", Price = 80 };
                var c1 = new RCustomer { CustomerName = "John" };
                var c2 = new RCustomer { CustomerName = "David" };
                var o1 = new ROrder { Active = true, Date = DateTime.Today, Customer = c1, Products = new List<RProduct>() { p1, p2 } };
                var o2 = new ROrder { Active = true, Date = new DateTime(2000, 1, 1), Products = new List<RProduct>() { p3 } };
                var o3 = new ROrder { Active = false, Date = new DateTime(2000, 1, 1), Customer = c2 };

                // insert 
                db.Insert<RProduct>(new RProduct[] { p1, p2, p3 });
                db.Insert<RCustomer>(new RCustomer[] { c1, c2 });
                db.Insert<ROrder>(new ROrder[] { o1, o2, o3 });

                // query
                var r1 = db.Query<ROrder>()
                    .Include(x => x.Customer)
                    .Where(x => x.Active)
                    .Where(x => x.Date == DateTime.Today)
                    .Where(x => x.Customer.CustomerId == c1.CustomerId)
                    .First();

                Assert.AreEqual(o1.Date, r1.Date);
                Assert.AreEqual(o1.Customer.CustomerName, r1.Customer.CustomerName);
                Assert.AreEqual(o1.Customer.CustomerName, r1.Customer.CustomerName);

                // check is exists
                var r2 = db.Query<ROrder>()
                    .Where(x => !x.Active)
                    .Exists();

                Assert.IsTrue(r2);

                // Single by id
                var r3 = db.Query<RCustomer>()
                    .SingleById(c2.CustomerId);

                Assert.AreEqual(c2.CustomerName, r3.CustomerName);

                // get second
                var r4 = db.Query<ROrder>()
                    .Include(x => x.Products)
                    .Where(x => x.Active)
                    .Skip(1)
                    .Limit(1)
                    .ToList();

                Assert.AreEqual(1, r4.Count);

            }
        }
    }
}