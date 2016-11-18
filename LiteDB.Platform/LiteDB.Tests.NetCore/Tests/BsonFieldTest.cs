using System;
using System.IO;
using LiteDB.Tests.NetCore;

namespace LiteDB.Tests
{
    public class MyBsonFieldTestClass
    {
        [BsonField("MY-STRING")]
        public string MyString { get; set; }

        [BsonField]
        internal string MyInternalPropertySerializable { get; set; }

        [BsonField]
        private string MyPrivatePropertySerializable { get; set; }

        [BsonField]
        protected string MyProtectedPropertySerializable { get; set; }

        [BsonField("INTERNAL-PROPERTY")]
        internal string MyInternalPropertyNamed { get; set; }

        [BsonField("PRIVATE-PROPERTY")]
        private string MyPrivatePropertyNamed { get; set; }

        [BsonField("PROTECTED-PROPERTY")]
        protected string MyProtectedPropertyNamed { get; set; }

        internal string MyInternalPropertyNotSerializable { get; set; }
        private string MyPrivatePropertyNotSerializable { get; set; }
        protected string MyProtectedPropertyNotSerializable { get; set; }

        public void SetPrivateProperties(string str)
        {
            MyPrivatePropertyNamed = str + "Named";
            MyPrivatePropertySerializable = str + "Serializable";
            MyPrivatePropertyNotSerializable = str + "NotSerialisable";
        }

        public void SetProtectedProperties(string str)
        {
            MyProtectedPropertyNamed = str + "Named";
            MyProtectedPropertySerializable = str + "Serializable";
            MyProtectedPropertyNotSerializable = str + "NotSerialisable";
        }

        public string GetMyPrivatePropertySerializable()
        {
            return MyPrivatePropertySerializable;
        }

        public string GetMyProtectedPropertySerializable()
        {
            return MyProtectedPropertySerializable;
        }

        public string GetMyPrivatePropertyNamed()
        {
            return MyPrivatePropertyNamed;
        }

        public string GetMyProtectedPropertyNamed()
        {
            return MyProtectedPropertyNamed;
        }

        public string GetMyPrivatePropertyNotSerializable()
        {
            return MyPrivatePropertyNotSerializable;
        }

        public string GetMyProtectedPropertyNotSerializable()
        {
            return MyProtectedPropertyNotSerializable;
        }
    }

    public class BsonFieldTest : TestBase
    {
        private MyBsonFieldTestClass CreateModel()
        {
            var c = new MyBsonFieldTestClass
            {
                MyString = "MyString",
                MyInternalPropertyNamed = "InternalPropertyNamed",
                MyInternalPropertyNotSerializable = "InternalPropertyNotSerializable",
                MyInternalPropertySerializable = "InternalPropertySerializable",
            };

            c.SetProtectedProperties("ProtectedProperties");
            c.SetPrivateProperties("PrivateProperty");

            return c;
        }

        public void BsonField_Test()
        {
            var test_name = "BsonField_Test";
            var mapper = new BsonMapper();
            mapper.UseLowerCaseDelimiter('_');

            var obj = CreateModel();
            var doc = mapper.ToDocument(obj);

            var json = JsonSerializer.Serialize(doc, true);
            var nobj = mapper.ToObject<MyBsonFieldTestClass>(doc);

            Helper.AssertIsTrue(test_name, 0, doc["MY-STRING"].AsString == obj.MyString);
            Helper.AssertIsTrue(test_name, 1, doc["INTERNAL-PROPERTY"].AsString == obj.MyInternalPropertyNamed);
            Helper.AssertIsTrue(test_name, 2, doc["PRIVATE-PROPERTY"].AsString == obj.GetMyPrivatePropertyNamed());
            Helper.AssertIsTrue(test_name, 3, doc["PROTECTED-PROPERTY"].AsString == obj.GetMyProtectedPropertyNamed());
            Helper.AssertIsTrue(test_name, 4, obj.MyString == nobj.MyString);
        }
    }
}