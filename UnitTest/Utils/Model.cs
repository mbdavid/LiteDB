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
}
