using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;

namespace LiteDB.Tests.Mapping
{
    [TestClass]
    public class MapperReadOnly
    {
        [TestMethod]
        public void MapGetOnlyCollection()
        {
            var mapper = new BsonMapper();
            mapper.UseLowerCaseDelimiter('_');

            var obj = CreateModel();
            var doc = mapper.ToDocument(obj);

            var json = JsonSerializer.Serialize(doc, true);

            var nobj = mapper.ToObject<CompositeObject>(doc);

            Assert.AreEqual(2, nobj.ReadOnlyCollection.Count);
            IList<SimpleObject> list = (IList<SimpleObject>)nobj.ReadOnlyCollection;
            Assert.AreEqual("Test one", list[0].Name);
            Assert.AreEqual("Test two", list[1].Name);

            IList<SimpleObject> enumeration = (IList<SimpleObject>)nobj.ReadOnlyEnumeration;
            Assert.AreEqual("Enum one", enumeration[0].Name);
            Assert.AreEqual("Enum two", enumeration[1].Name);

            Assert.AreEqual(0, nobj.ReadOnlyArray.Length);
        }

        [TestMethod]
        public void MapReadOnlyCollection()
        {
            var mapper = new BsonMapper();
            mapper.UseLowerCaseDelimiter('_');

            var obj = CreateReadOnlyModel();
            var doc = mapper.ToDocument(obj);

            var json = JsonSerializer.Serialize(doc, true);

            var nobj = mapper.ToObject<ReadOnlyCompositeObject>(doc);

            Assert.AreEqual(3, nobj.ReadOnlyInWrapperWithSetter.Count);
            Assert.AreEqual(3, nobj.ReadOnlyInWrapper.Count);
        }

        private CompositeObject CreateModel()
        {
            var obj = new CompositeObject();
            obj.ReadOnlyCollection.Add(new SimpleObject() { Name = "Test one" });
            obj.ReadOnlyCollection.Add(new SimpleObject() { Name = "Test two" });

            ((IList)obj.ReadOnlyEnumeration).Add(new SimpleObject() { Name = "Enum one" });
            ((IList)obj.ReadOnlyEnumeration).Add(new SimpleObject() { Name = "Enum two" });
            return obj;
        }

        private ReadOnlyCompositeObject CreateReadOnlyModel()
        {
            var model = new ReadOnlyCompositeObject();
            model.ReadOnlyInWrapperWithSetter = new ReadOnlyCollection<int>(new List<int>() { 1, 2});
            return model;
        }
    }

    public class ReadOnlyCompositeObject
    {
        private ReadOnlyCollection<int> read = new ReadOnlyCollection<int>(new List<int>() { 1,2,3});
        public ReadOnlyCollection<int> ReadOnlyInWrapper
        {
            get
            {
                return read;
            }
        }

        public ReadOnlyCollection<int> ReadOnlyInWrapperWithSetter { get; set; }
    }

    public class CompositeObject
    {
        private ICollection<SimpleObject> _readOnlyCollection;

        private IEnumerable<SimpleObject> _readOnlyEnumeration;

        private SimpleObject[] _readOnlyArray;

        public ICollection<SimpleObject> ReadOnlyCollection
        {
            get
            {
                if (_readOnlyCollection == null)
                {
                    _readOnlyCollection = new List<SimpleObject>();
                }
                return _readOnlyCollection;
            }
            private set { _readOnlyCollection = value; }
        }

        public IEnumerable<SimpleObject> ReadOnlyEnumeration
        {
            get
            {
                if (_readOnlyEnumeration == null)
                {
                    _readOnlyEnumeration = new List<SimpleObject>();
                }
                return _readOnlyEnumeration;
            }
            private set { _readOnlyEnumeration = value; }
        }

        public SimpleObject[] ReadOnlyArray {
            get {
                if (_readOnlyArray == null)
                {
                    _readOnlyArray = new SimpleObject[0];
                }
                return _readOnlyArray;
            }
            set { _readOnlyArray = value; }
        }
    }

    public class SimpleObject
    {
        public string Name { get; set; }
    }
}
