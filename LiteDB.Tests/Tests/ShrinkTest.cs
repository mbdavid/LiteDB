using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
    [TestClass]
    public class ShrinkTest : TestBase
    {
        private const int Iterations = 10;
        private readonly string _customerName;
        private readonly string _productName;

        public ShrinkTest()
        {
            _customerName = "Jo"+new string('h', 1000)+"n Doe";
            _productName = "Prod"+new string('u', 1000)+"ct";
        }

        [TestMethod]
        public void Shrink_None_Test()
        {
            double sizeAfterCreation, sizeAfterShrink;

            using (var tmp = new TempFile())
            {
                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    db.GetCollection<Customer>("customers");

                    sizeAfterCreation = GetLocalDbSizeMegabytes(tmp.Filename);

                    db.Shrink();

                    sizeAfterShrink = GetLocalDbSizeMegabytes(tmp.Filename);
                }
            }

            var shrinkRatio = sizeAfterShrink / sizeAfterCreation;
            Assert.IsTrue(shrinkRatio <= 1.0);
        }

        [TestMethod]
        public void Shrink_One_Test()
        {
            double sizeAfterInserts, sizeAfterShrink;

            using (var tmp = new TempFile())
            {
                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    var customers = db.GetCollection<Customer>("customers");

                    Create(customers, id => new Customer { Id = id, Name = ""+_customerName+id });

                    sizeAfterInserts = GetLocalDbSizeMegabytes(tmp.Filename);

                    Delete(customers);

                    db.Shrink();

                    sizeAfterShrink = GetLocalDbSizeMegabytes(tmp.Filename);
                }
            }

            var shrinkRatio = sizeAfterShrink / sizeAfterInserts;
            Assert.IsTrue(shrinkRatio < 0.5);
        }

        [TestMethod]
        public void Shrink_Many_Test()
        {
            double sizeAfterInserts, sizeAfterShrink;

            using (var tmp = new TempFile())
            {
                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    var products = db.GetCollection<Product>("products");
                    var customers = db.GetCollection<Customer>("customers");

                    Create(products, id => new Product { ProductId = id, Name = ""+_customerName+id });
                    Create(customers, id => new Customer { Id = id, Name = "" + _customerName + id });

                    sizeAfterInserts = GetLocalDbSizeMegabytes(tmp.Filename);

                    Delete(customers);
                    Delete(products);

                    db.Shrink();

                    sizeAfterShrink = GetLocalDbSizeMegabytes(tmp.Filename);
                }
            }

            var shrinkRatio = sizeAfterShrink/sizeAfterInserts;
            Assert.IsTrue(shrinkRatio < 0.5);
        }

        private static void Create<T>(LiteCollection<T> col, Func<int,T> factory) where T : class, new()
        {
            for (var i = 0; i < Iterations; i++)
            {
                var item = factory(i + 1);
                col.Insert(item);
            }
        }

        private static void Delete(LiteCollection<Product> col) 
        {
            for (var j = 0; j < Iterations; j++)
            {
                var id = j + 1;
                col.Delete(x => x.ProductId == id);
            }
        }

        private static void Delete(LiteCollection<Customer> col)
        {
            for (var j = 0; j < Iterations; j++)
            {
                var id = j + 1;
                col.Delete(x => x.Id == id);
            }
        }

        private static double GetLocalDbSizeMegabytes(string filename)
        {
            const double megabyteSize = 1024.0 * 1024.0;
            var dbFile = new FileInfo(filename);
            var size = dbFile.Length / megabyteSize;
            return size;
        }
    }
}