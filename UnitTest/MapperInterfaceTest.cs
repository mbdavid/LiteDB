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

    [TestClass]
    public class MapperInterfaceTest
    {
        public class ClassWithInterface {
            [BsonId]
            public String name {get;set;}

            public MyInterface myInterface { get; set; }
        }

        public interface MyInterface {
            String getName();
        }

        public class Impl1 : MyInterface {
            public String getName() {
                return "name1";
            }
        }

        public class Impl2 : MyInterface {
            public String getName() {
                return "name1";
            }
        }

        private ClassWithInterface CreateModel(MyInterface implementation)
        {
            var c = new ClassWithInterface
            {
                name = "Test",
                myInterface = implementation
            };

            return c;
        }

        [TestMethod]
        public void MapperInterface_Test()
        {
            var mapper = new BsonMapper();

            ClassWithInterface objWithImpl1 = CreateModel(new Impl1());
            BsonDocument doc = mapper.ToDocument(objWithImpl1);
            ClassWithInterface mappedObjectWithImpl1 = mapper.ToObject<ClassWithInterface>(doc);

            Assert.AreEqual(objWithImpl1.myInterface, mappedObjectWithImpl1.myInterface);
            Assert.AreEqual(objWithImpl1.myInterface.getName(), mappedObjectWithImpl1.myInterface.getName());
        }
    }
}
