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

        public class ClassWithListInterface {
            [BsonId]
            public String name { get; set; }

            public List<MyInterface> interfaces { get; set; }
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
                return "name2";
            }
        }

        public class ImplWithProperty : MyInterface {
            public String name { get; set; }
            public String getName() {
                return name;
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

        const string RANDOM_NAME = "NAME,WHATEVER";

        [TestMethod]
        public void WhenCreatingClassWithImplementationShouldGenerateSameImplementation()
        {
            var mapper = new BsonMapper();

            ClassWithInterface objWithImpl1 = CreateModel(new Impl1());
            ClassWithInterface mappedObjectWithImpl1 = MapAndDemapObject(mapper, objWithImpl1);

            Assert.AreEqual(objWithImpl1.myInterface.getName(), mappedObjectWithImpl1.myInterface.getName());
        }

        [TestMethod]
        public void WhenCreatingTwoImplementsShouldHaveDifferentMethods() {
            var mapper = new BsonMapper();

            ClassWithInterface objWithImpl1 = CreateModel(new Impl1());
            ClassWithInterface mappedObjectWithImpl1 = MapAndDemapObject(mapper, objWithImpl1);

            ClassWithInterface objWithImpl2 = CreateModel(new Impl2());
            ClassWithInterface mappedObjectWithImpl2 = MapAndDemapObject(mapper, objWithImpl2);

            Assert.AreNotEqual(mappedObjectWithImpl1.myInterface.getName(), mappedObjectWithImpl2.myInterface.getName());
        }

        [TestMethod]
        public void WhenCreatingImplementWithPropertyShouldKeepProperty() {
            var mapper = new BsonMapper();

            ClassWithInterface objWithImpl = CreateModel(new ImplWithProperty() { name = RANDOM_NAME});
            ClassWithInterface mappedObjectWithImpl = MapAndDemapObject(mapper, objWithImpl);

            Assert.AreEqual(RANDOM_NAME, mappedObjectWithImpl.myInterface.getName());
        }

        [TestMethod]
        public void WhenCreatingClassWithListOfInterfaceShouldHaveAllImplements() {
            var mapper = new BsonMapper();

            List<MyInterface> interfaces = new List<MyInterface>() {
                new Impl1(),
                new Impl2(),
                new ImplWithProperty() {
                    name = RANDOM_NAME
                }
            };

            ClassWithListInterface obj = new ClassWithListInterface(){ interfaces = interfaces};
            ClassWithListInterface mappedObject = MapAndDemapObject(mapper, obj);

            Assert.AreEqual(obj.interfaces[0].getName(), mappedObject.interfaces[0].getName());
            Assert.AreEqual(obj.interfaces[1].getName(), mappedObject.interfaces[1].getName());
            Assert.AreEqual(obj.interfaces[2].getName(), mappedObject.interfaces[2].getName());
        }

        private ClassWithListInterface MapAndDemapObject(BsonMapper mapper, ClassWithListInterface obj) {
            return mapper.ToObject<ClassWithListInterface>(mapper.ToDocument(obj));
        }

        private ClassWithInterface MapAndDemapObject(BsonMapper mapper, ClassWithInterface obj) {
            return mapper.ToObject<ClassWithInterface>(mapper.ToDocument(obj));
        }
    }
}
