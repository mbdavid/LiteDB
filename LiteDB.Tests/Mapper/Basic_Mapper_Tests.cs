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

    public class CustomStringEnumerable : IEnumerable<string>
    {
        private readonly List<string> innerList;

        public CustomStringEnumerable()
        {
            innerList = new List<string>();
        }

        public CustomStringEnumerable(IEnumerable<string> list)
        {
            innerList = new List<string>(list);
        }

        public void Add(string item)
        {
            innerList.Add(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public enum MyEnum { First, Second }

    public class MyClass
    {
        [BsonId(false)]
        public int MyId { get; set; }

        [BsonField("MY-STRING")]
        public string MyString { get; set; }

        public Guid MyGuid { get; set; }
        public DateTime MyDateTime { get; set; }
        public DateTime? MyDateTimeNullable { get; set; }
        public int? MyIntNullable { get; set; }
        public MyEnum MyEnumProp { get; set; }
        public char MyChar { get; set; }
        public byte MyByte { get; set; }
        public sbyte MySByte { get; set; }
        public TimeSpan MyTimespan { get; set; }

        public decimal MyDecimal { get; set; }

        public decimal? MyDecimalNullable { get; set; }

        public Uri MyUri { get; set; }

        // do not serialize this properties
        [BsonIgnore]
        public string MyIgnore { get; set; }

        public string MyReadOnly { get { return "read only"; } }
        public string MyWriteOnly { set; private get; }

        // lists
        public string[] MyStringArray { get; set; }

        public List<string> MyStringList { get; set; }
        public IEnumerable<string> MyStringEnumerable { get; set; }
        public CustomStringEnumerable CustomStringEnumerable { get; set; }
        public Dictionary<int, string> MyDict { get; set; }
        public Dictionary<StringComparison, string> MyDictEnum { get; set; }

        // list of structs
        public ICollection<Point> MyCollectionPoint { get; set; }
        public IList<Point> MyListPoint { get; set; }
        public IEnumerable<Point> MyEnumerablePoint { get; set; }

        // interfaces
        public IMyInterface MyInterface { get; set; }

        public List<IMyInterface> MyListInterface { get; set; }
        public IList<IMyInterface> MyIListInterface { get; set; }

        // objects
        public object MyObjectString { get; set; }

        public object MyObjectInt { get; set; }
        public object MyObjectImpl { get; set; }
        public List<object> MyObjectList { get; set; }

        // fields
        public string MyField;


        // this is a indexer property - should not be serialized #795
        public string this[string itemName]
        {
            get
            {
                return this.MyString;
            }
            set
            {
                this.MyString = value;
            }
        }

    }

    public interface IMyInterface
    {
        string Name { get; set; }
    }

    public class MyImpl : IMyInterface
    {
        public string Name { get; set; }
    }

    public class MyFluentEntity
    {
        public int MyPrimaryPk { get; set; }
        public string CustomName { get; set; }
        public bool DoNotIncludeMe { get; set; }
        public DateTime MyDateIndexed { get; set; }
    }

    #endregion

    [TestClass]
    public class Basic_Mapper_Tests
    {
        private MyClass CreateModel()
        {
            var c = new MyClass
            {
                MyId = 123,
                MyString = "John",
                MyGuid = Guid.NewGuid(),
                MyDateTime = DateTime.Now,
                //MyProperty = "SerializeTHIS",
                MyIgnore = "IgnoreTHIS",
                MyIntNullable = 999,
                MyStringList = new List<string>() { "String-1", "String-2" },
                MyWriteOnly = "write-only",
                //MyInternalProperty = "internal-field",
                MyDict = new Dictionary<int, string>() { { 1, "Row1" }, { 2, "Row2" } },
                MyDictEnum = new Dictionary<StringComparison, string>() { { StringComparison.Ordinal, "ordinal" } },
                MyStringArray = new string[] { "One", "Two" },
                MyStringEnumerable = new string[] { "One", "Two" },
                CustomStringEnumerable = new CustomStringEnumerable(new string[] { "One", "Two" }),

                // list of structs
                MyCollectionPoint = new List<Point> { new Point(1, 1), Point.Empty },
                MyListPoint = new List<Point> { new Point(1, 1), Point.Empty },
                MyEnumerablePoint = new[] { new Point(1, 1), Point.Empty },

                MyEnumProp = MyEnum.Second,
                MyChar = 'Y',
                MyUri = new Uri("http://www.numeria.com.br"),
                MyByte = 255,
                MySByte = -99,
                MyField = "Field test",
                MyTimespan = TimeSpan.FromDays(1),
                MyDecimal = 19.9m,
                MyDecimalNullable = 25.5m,

                MyInterface = new MyImpl { Name = "John" },
                MyListInterface = new List<IMyInterface>() { new MyImpl { Name = "John" } },
                MyIListInterface = new List<IMyInterface>() { new MyImpl { Name = "John" } },

                MyObjectString = "MyString",
                MyObjectInt = 123,
                MyObjectImpl = new MyImpl { Name = "John" },
                MyObjectList = new List<object>() { 1, "ola", new MyImpl { Name = "John" }, new Uri("http://www.cnn.com") }
            };

            return c;
        }

        public BsonMapper CreateMapper()
        {
            var mapper = new BsonMapper();
            mapper.UseLowerCaseDelimiter('_');
            mapper.IncludeFields = true;
            return mapper;
        }

        [TestMethod]
        public void Basic_Mapper()
        {
            var mapper = CreateMapper();
            var obj = CreateModel();
            var doc = mapper.ToDocument(obj);

            var json = JsonSerializer.Serialize(doc, true);

            // test read-only
            Assert.AreEqual(obj.MyReadOnly, doc["my_read_only"].AsString);

            var nobj = mapper.ToObject<MyClass>(doc);

            // compare object to document
            Assert.AreEqual(doc["_id"].AsInt32, obj.MyId);
            Assert.AreEqual(doc["MY-STRING"].AsString, obj.MyString);
            Assert.AreEqual(doc["my_guid"].AsGuid, obj.MyGuid);

            // compare 2 objects
            Assert.AreEqual(obj.MyId, nobj.MyId);
            Assert.AreEqual(obj.MyString, nobj.MyString);
            Assert.AreEqual(obj.MyGuid, nobj.MyGuid);
            Assert.AreEqual(obj.MyDateTime.ToString(), nobj.MyDateTime.ToString());
            Assert.AreEqual(obj.MyDateTimeNullable, nobj.MyDateTimeNullable);
            Assert.AreEqual(obj.MyIntNullable, nobj.MyIntNullable);
            Assert.AreEqual(obj.MyEnumProp, nobj.MyEnumProp);
            Assert.AreEqual(obj.MyChar, nobj.MyChar);
            Assert.AreEqual(obj.MyByte, nobj.MyByte);
            Assert.AreEqual(obj.MySByte, nobj.MySByte);
            Assert.AreEqual(obj.MyField, nobj.MyField);
            Assert.AreEqual(obj.MyTimespan, nobj.MyTimespan);
            Assert.AreEqual(obj.MyDecimal, nobj.MyDecimal);
            Assert.AreEqual(obj.MyUri, nobj.MyUri);

            // list
            Assert.AreEqual(obj.MyStringArray[0], nobj.MyStringArray[0]);
            Assert.AreEqual(obj.MyStringArray[1], nobj.MyStringArray[1]);
            Assert.AreEqual(obj.MyStringEnumerable.First(), nobj.MyStringEnumerable.First());
            Assert.AreEqual(obj.MyStringEnumerable.Take(1).First(), nobj.MyStringEnumerable.Take(1).First());
            Assert.AreEqual(true, obj.CustomStringEnumerable.SequenceEqual(nobj.CustomStringEnumerable));
            Assert.AreEqual(obj.MyDict[2], nobj.MyDict[2]);
            Assert.AreEqual(obj.MyDictEnum[StringComparison.Ordinal], nobj.MyDictEnum[StringComparison.Ordinal]);

            // list of structs
            Assert.AreEqual(obj.MyCollectionPoint.First(), nobj.MyCollectionPoint.First());
            Assert.AreEqual(obj.MyListPoint.First(), nobj.MyListPoint.First());
            Assert.AreEqual(obj.MyEnumerablePoint.First(), nobj.MyEnumerablePoint.First());

            // interfaces
            Assert.AreEqual(obj.MyInterface.Name, nobj.MyInterface.Name);
            Assert.AreEqual(obj.MyListInterface[0].Name, nobj.MyListInterface[0].Name);
            Assert.AreEqual(obj.MyIListInterface[0].Name, nobj.MyIListInterface[0].Name);

            // objects
            Assert.AreEqual(obj.MyObjectString, nobj.MyObjectString);
            Assert.AreEqual(obj.MyObjectInt, nobj.MyObjectInt);
            Assert.AreEqual((obj.MyObjectImpl as MyImpl).Name, (nobj.MyObjectImpl as MyImpl).Name);
            Assert.AreEqual(obj.MyObjectList[0], obj.MyObjectList[0]);
            Assert.AreEqual(obj.MyObjectList[1], obj.MyObjectList[1]);
            Assert.AreEqual(obj.MyObjectList[3], obj.MyObjectList[3]);
        }
    }
}