using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.ImplicitCollectionName
{
    [TestClass]
    public class BasicTestsOnImplicitCollectionNames
    {
        public class BasicObject
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [TestMethod]
        public void WillAssignCollectionNameIfNotSpecified()
        {
            using (var stream = new MemoryStream())
            {
                using (var db = new LiteDatabase(stream))
                {
                    var objects = db.GetCollection<BasicObject>();
                    objects.Insert(new BasicObject());
                }

                using (var db = new LiteDatabase(stream))
                {
                    var actualCollectionNames = db.GetCollectionNames();
                    var expectedCollectionNames = new[] {"BasicObjectCollection"};

                    actualCollectionNames.ShouldAllBeEquivalentTo(expectedCollectionNames);
                }
            }
            
        }

        [TestMethod]
        public void WillGetTheCorrectObjectFromImplicitlyNamedCollection()
        {
            using (var stream = new MemoryStream())
            {
                using (var db = new LiteDatabase(stream))
                {
                    var objects = db.GetCollection<BasicObject>();
                    objects.Insert(new BasicObject {Name="BasicObject"});
                }

                using (var db = new LiteDatabase(stream))
                {
                    var expectedObjects = new[] {new BasicObject {Id=1, Name = "BasicObject"}};
                    var actualObjects = db.GetCollection<BasicObject>().FindAll();

                    actualObjects.ShouldAllBeEquivalentTo(expectedObjects);
                }
            }

        }

    }

}
