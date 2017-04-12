using LiteDB;
using liteDBSolution.DbReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class DbReference
    {
    
        private LiteDatabase LiteDataBase;
        private string DbPath = @"MyData.db";
        
        public void mappingRef()
        {
            using (LiteDataBase = new LiteDatabase(DbPath))
            {
                var customer = new StudentComponent()
                {
                    StudentId = 123,
                    AddressDetails = new Address() { AddressId=1234, pincode=567890,State="AP",Street1="#158",Street2="KRPuram" },
                    StudentName = "Maruthi"
                };
                
                var doc = BsonMapper.Global.ToDocument<Student>(student);

                var collection = LiteDataBase.GetCollection<BsonDocument>("StudentDocument");

                collection.Insert(doc);
            }
    
    }
    
    public class Student
    {
        [BsonId]
        public int StudentId { get; set; }
        [BsonField]
        public string StudentName { get; set; }

        [BsonRef("Address")]
        public Address AddressDetails { get; set; }
    }
    
    public class Address
    {
        [BsonId]
        public int AddressId { get; set; }
        [BsonField]
        public string Street1 { get; set; }
        [BsonField]
        public string Street2 { get; set; }
        [BsonField]
        public string State { get; set; }
        [BsonField]
        public long pincode { get; set; }
    }
