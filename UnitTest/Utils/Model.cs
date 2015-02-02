using System;
using System.Collections.Generic;
using LiteDB;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    public class Customer
    {
        [BsonId]
        public Guid CustomerId { get; set; }

        public string Name { get; set; }

        public DateTime CreationDate { get; set; }

        public Customer()
        {
        }
    }

    public class Order
    {
        [BsonId]
        public int OrderKey { get; set; }

        public DateTime Date { get; set; }

        public Guid CustomerId { get; set; }

        [BsonIgnore]
        public Customer Customer { get; set; }

        public List<OrderItem> Items { get; set; }

        public Order()
        {
            this.Date = DateTime.Now;
            this.Items = new List<OrderItem>();
        }
    }

    public class OrderItem
    {
        public int Qtd { get; set; }

        public string Description { get; set; }

        public decimal Unit { get; set; }

        public decimal Total { get { return this.Qtd * this.Unit; } }
    }

    public class Post
    {
        [BsonId]
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime PostDate { get; set; }
        public string Body { get; set; }

        public static List<Post> GetData(int count)
        {
            var posts = new List<Post>();
            var rnd = new Random(DateTime.Now.Millisecond);

            for (var i = 1; i <= count; i++)
            {
                var p = new Post();
                p.Id = i;
                p.Title = DB.LoremIpsum(5, 12, 1, 1, 1).Trim();
                p.PostDate = DateTime.Now.AddDays(-rnd.Next(0, 1000));
                p.Body = DB.LoremIpsum(20, 30, 5, 10, 2);

                posts.Add(p);
            }

            return posts;
        }
    }
}
