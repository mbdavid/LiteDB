using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;

namespace LiteDB.Tests.Mapper
{
    #region Model

    public class GetterSetterClass
    {
        public string PublicProperty { get; set; }
        internal string InternalProperty { get; set; }
        protected string ProtectedProperty { get; set; }
        private string PrivateProperty { get; set; }

        public string PublicField;
        internal string InternalField;
        protected string ProtectedField;
        private string PrivateField;

        public string GetProtectedProperty()
        {
            return ProtectedProperty;
        }

        public string GetPrivateProperty()
        {
            return PrivateProperty;
        }

        public void SetProtectedProperty(string value)
        {
            ProtectedProperty = value;
        }

        public void SetPrivateProperty(string value)
        {
            PrivateProperty = value;
        }

        public string GetProtectedField()
        {
            return ProtectedField;
        }

        public string GetPrivateField()
        {
            return PrivateField;
        }

        public void SetProtectedField(string value)
        {
            ProtectedField = value;
        }

        public void SetPrivateField(string value)
        {
            PrivateField = value;
        }
    }

    public struct GetterSetterStruct
    {
        public string PublicProperty { get; set; }
        internal string InternalProperty { get; set; }
        private string PrivateProperty { get; set; }

        public string PublicField;
        internal string InternalField;
        private string PrivateField;

        public string GetPrivateProperty()
        {
            return PrivateProperty;
        }

        public void SetPrivateProperty(string value)
        {
            PrivateProperty = value;
        }

        public string GetPrivateField()
        {
            return PrivateField;
        }

        public void SetPrivateField(string value)
        {
            PrivateField = value;
        }
    }

    #endregion

    [TestClass]
    public class Reflection_Getter_Setter_Tests
    {
        [TestMethod]
        public void Getter_Setter_Classes()
        {
            var o = new GetterSetterClass
            {
                PublicProperty = "PublicProperty",
                InternalProperty = "InternalProperty",

                PublicField = "PublicField",
                InternalField = "InternalField"
            };

            o.SetProtectedProperty("ProtectedProperty");
            o.SetPrivateProperty("PrivateProperty");

            o.SetProtectedField("ProtectedField");
            o.SetPrivateField("PrivateField");

            var m = new BsonMapper
            {
                IncludeFields = true
            };

            m.IncludeNonPublic = true;

            var clone = m.ToObject<GetterSetterClass>(m.ToDocument<GetterSetterClass>(o));

            Assert.AreEqual(o.PublicProperty, clone.PublicProperty);
            Assert.AreEqual(o.InternalProperty, clone.InternalProperty);

            Assert.AreEqual(o.PublicField, clone.PublicField);
            Assert.AreEqual(o.InternalField, clone.InternalField);

            Assert.AreEqual(o.GetProtectedProperty(), clone.GetProtectedProperty());
            Assert.AreEqual(o.GetProtectedField(), clone.GetProtectedField());

            Assert.AreEqual(o.GetPrivateProperty(), clone.GetPrivateProperty());
            Assert.AreEqual(o.GetPrivateField(), clone.GetPrivateField());
        }

        [TestMethod]
        public void Getter_Setter_Structs()
        {
            var o = new GetterSetterStruct
            {
                PublicProperty = "PublicProperty",
                InternalProperty = "InternalProperty",

                PublicField = "PublicField",
                InternalField = "InternalField"
            };

            o.SetPrivateProperty("PrivateProperty");

            o.SetPrivateField("PrivateField");

            var m = new BsonMapper
            {
                IncludeFields = true
            };

            m.IncludeNonPublic = true;

            var clone = m.ToObject<GetterSetterStruct>(m.ToDocument<GetterSetterStruct>(o));

            Assert.AreEqual(o.PublicProperty, clone.PublicProperty);
            Assert.AreEqual(o.InternalProperty, clone.InternalProperty);

            Assert.AreEqual(o.PublicField, clone.PublicField);
            Assert.AreEqual(o.InternalField, clone.InternalField);

            Assert.AreEqual(o.GetPrivateProperty(), clone.GetPrivateProperty());
            Assert.AreEqual(o.GetPrivateField(), clone.GetPrivateField());
        }
    }
}