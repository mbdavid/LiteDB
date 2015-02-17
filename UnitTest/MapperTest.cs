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
    public class MyClass
    {
        public int Id { get; set; }
        public string MyString { get; set; }
        public Guid MyGuid { get; set; }
        public DateTime MyDateTime { get; set; }
        public DateTime? MyDateTimeNullable { get; set; }
        public int? MyIntNullable { get; set; }

        // do not serialize this properties
        [BsonIgnore]
        public string MyIgnore { get; set; }
        public string MyReadOnly { get; private set; }
        public string MyWriteOnly { set; private get; }
        public string MyField = "DoNotSerializeThis";
        internal string MyInternalProperty { get; set; }

        // special types
        public NameValueCollection MyNameValueCol { get; set; }

        // lists
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
                MyIgnore = "IgnoreTHIS",
                MyIntNullable = 999,
                MyStringList = new List<string>(),
                MyWriteOnly = "write-only",
                MyInternalProperty = "internal-field",
                MyNameValueCol = new NameValueCollection(),
                MyDict = new Dictionary<int,string>()
            };

            c.MyStringList.Add("String-1");
            c.MyStringList.Add("String-2");

            c.MyNameValueCol["key-1"] = "value-1";
            c.MyNameValueCol["KeyNumber2"] = "value-2";

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
    }
}
