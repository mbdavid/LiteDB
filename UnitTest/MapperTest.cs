using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;

namespace UnitTest
{
    public enum MyEnum { First, Second }

    public class MyClass
    {
        public int Id { get; set; }
        [BsonProperty("MY-STRING")]
        public string MyString { get; set; }
        public Guid MyGuid { get; set; }
        public DateTime MyDateTime { get; set; }
        public DateTime? MyDateTimeNullable { get; set; }
        public int? MyIntNullable { get; set; }
        public MyEnum MyEnumProp { get; set; }
        //public char MyChar { get; set; }
        public byte MyByte { get; set; }

        // do not serialize this properties
        [BsonIgnore]
        public string MyIgnore { get; set; }
        public string MyReadOnly { get; private set; }
        public string MyWriteOnly { set; private get; }
        public string MyField = "DoNotSerializeThis";
        internal string MyInternalProperty { get; set; }

        // special types
        public NameValueCollection MyNameValueCollection { get; set; }

        // lists
        public string[] MyStringArray { get; set; }
        public List<string> MyStringList { get; set; }
        public Dictionary<int, string> MyDict { get; set; }

    }


    [TestClass]
    public class MapperTest
    {
        private MyClass CreateModel()
        {
            var c = new MyClass
            {
                Id = 123,
                MyString = "John",
                MyGuid = Guid.NewGuid(),
                MyDateTime = DateTime.Now,
                MyIgnore = "IgnoreTHIS",
                MyIntNullable = 999,
                MyStringList = new List<string>(),
                MyWriteOnly = "write-only",
                MyInternalProperty = "internal-field",
                MyNameValueCollection = new NameValueCollection(),
                MyDict = new Dictionary<int,string>(),
                MyStringArray = new string[] { "One", "Two" },
                MyEnumProp = MyEnum.Second,
                //MyChar = 'Y',
                MyByte = 255
            };

            c.MyStringList.Add("String-1");
            c.MyStringList.Add("String-2");

            c.MyNameValueCollection["key-1"] = "value-1";
            c.MyNameValueCollection["KeyNumber2"] = "value-2";

            c.MyDict[1] = "Row 1";
            c.MyDict[2] = "Row 2";

            return c;
        }

        [TestMethod]
        public void Mapper_Test()
        {
            var o = CreateModel();
            var mapper = new BsonMapper();
            mapper.UseLowerCaseDelimiter();

            var doc = mapper.ToDocument(o);

            var json = JsonSerializer.Serialize(doc, true);

            Debug.Print(json);

            var n = mapper.ToObject<MyClass>(doc);

            Assert.AreEqual(doc.Id, 123);
            //Assert.AreEqual(d["_id"].AsInt64, o["_id"].AsInt64);

        }

        [TestMethod]
        public void MapperPerf_Test()
        {
            var model = CreateModel();
            var mapper = new BsonMapper();
            mapper.UseLowerCaseDelimiter();
            var size = 100000;

            // Cache before
            var doc = mapper.ToDocument(model);
            var bytesBson = BsonSerializer.Serialize(doc);
            var bytesJson = fastBinaryJSON.BJSON.ToBJSON(model);


            Debug.Print("--------------------------");

            var sm = Stopwatch.StartNew();

            // .NET Class to BsonDocument
            for (var i = 0; i < size; i++)
            {
                mapper.ToDocument(model);
            }

            sm.Stop();

            Debug.Print(".NET Class to BsonDocument = " + sm.ElapsedMilliseconds);

            sm.Restart();

            for (var i = 0; i < size; i++)
            {
                BsonSerializer.Serialize(doc);
            }

            Debug.Print("BsonDocument to BsonBytes = " + sm.ElapsedMilliseconds);

            sm.Restart();

            for (var i = 0; i < size; i++)
            {
                fastBinaryJSON.BJSON.ToBJSON(model);
            }

            Debug.Print("fastBinaryJson (mapper+serialize) = " + sm.ElapsedMilliseconds);

            Debug.Print("=====================");

            sm.Restart();

            // BsonDocument to .NET class
            for (var i = 0; i < size; i++)
            {
                mapper.ToObject<MyClass>(doc);
            }

            sm.Stop();

            Debug.Print("BsonDocument to .NET Class = " + sm.ElapsedMilliseconds);

            sm.Restart();

            for (var i = 0; i < size; i++)
            {
                BsonSerializer.Deserialize(bytesBson);
            }

            Debug.Print("BsonBytes to BsonDocument = " + sm.ElapsedMilliseconds);


            sm.Restart();

            for (var i = 0; i < size; i++)
            {
                fastBinaryJSON.BJSON.ToObject<MyClass>(bytesJson);
            }

            Debug.Print("fastBinaryJson (mapper+deserialize) = " + sm.ElapsedMilliseconds);

        }


    }
}
