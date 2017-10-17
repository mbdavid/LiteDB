using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.Mapper
{
    #region Model

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

    #endregion

    [TestClass]
    public class Non_Public_Members_Tests
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

        [TestMethod]
        public void Non_Public_Members()
        {
            var mapper = new BsonMapper();
            mapper.UseLowerCaseDelimiter('_');
            mapper.IncludeNonPublic = true;

            var obj = CreateModel();
            var doc = mapper.ToDocument(obj);

            var json = JsonSerializer.Serialize(doc, true);
            var nobj = mapper.ToObject<MyBsonFieldTestClass>(doc);

            Assert.AreEqual(doc["MY-STRING"].AsString, obj.MyString);
            Assert.AreEqual(doc["INTERNAL-PROPERTY"].AsString, obj.MyInternalPropertyNamed);
            Assert.AreEqual(doc["PRIVATE-PROPERTY"].AsString, obj.GetMyPrivatePropertyNamed());
            Assert.AreEqual(doc["PROTECTED-PROPERTY"].AsString, obj.GetMyProtectedPropertyNamed());
            Assert.AreEqual(obj.MyString, nobj.MyString);

            //Internal
            Assert.AreEqual(obj.MyInternalPropertyNamed, nobj.MyInternalPropertyNamed);
            Assert.AreEqual(obj.MyInternalPropertySerializable, nobj.MyInternalPropertySerializable);
            // Assert.AreEqual(nobj.MyInternalPropertyNotSerializable, null);
            //Private
            Assert.AreEqual(obj.GetMyPrivatePropertyNamed(), nobj.GetMyPrivatePropertyNamed());
            Assert.AreEqual(obj.GetMyPrivatePropertySerializable(), nobj.GetMyPrivatePropertySerializable());
            // Assert.AreEqual(nobj.GetMyPrivatePropertyNotSerializable(), null);
            //protected
            Assert.AreEqual(obj.GetMyProtectedPropertyNamed(), nobj.GetMyProtectedPropertyNamed());
            Assert.AreEqual(obj.GetMyProtectedPropertySerializable(), nobj.GetMyProtectedPropertySerializable());
            //Assert.AreEqual(nobj.GetMyProtectedPropertyNotSerializable(), null);
        }
    }
}
