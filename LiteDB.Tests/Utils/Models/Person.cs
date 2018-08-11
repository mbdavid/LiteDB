using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    // { 
    //     "name": "Kelsey Garza", 
    //     "age": 66, 
    //     "phone": "624-744-6218", 
    //     "email": "Kelly@suscipit.edu", 
    //     "address": "62702 West Bosnia and Herzegovina Way", 
    //     "city": "Wheaton", 
    //     "state": "MO", 
    //     "date": { "$date": "1950-08-07"}, 
    //     "active": true
    // }
    public class Person : IEqualityComparer<Person>, IComparable<Person>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public DateTime Date { get; set; }
        public bool Active { get; set; }

        public int CompareTo(Person other)
        {
            return this.Id.CompareTo(other.Id);
        }

        public bool Equals(Person x, Person y)
        {
            return x.Id == y.Id &&
                x.Name == y.Name &&
                x.Age == y.Age &&
                x.Phone == y.Phone &&
                x.Email == y.Email &&
                x.Address == y.Address &&
                x.City == y.City &&
                x.State == y.State &&
                x.Date == y.Date &&
                x.Active == y.Active;
        }

        public int GetHashCode(Person obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}