using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.Mapper
{
    #region Model

    public class PersonModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        #region Overrides of Object

        /// <summary>
        /// Serves as the default hash function. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            var hash = 13;
            hash = hash + FirstName.GetHashCode()*7;
            hash = hash + LastName.GetHashCode()*7;

            return hash;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;

            var roleModel = obj as PersonModel;
            if (roleModel == null) return false;

            var equal = string.Equals(FirstName, roleModel.FirstName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(LastName, roleModel.LastName, StringComparison.OrdinalIgnoreCase);

            return equal;
        }

        #endregion
    }

    public class RoleModel
    {
        public RoleModel()
        {
            Persons = new Dictionary<Guid, PersonModel>();
        }

        [BsonId]
        public string Name { get; set; }
        public Dictionary<Guid, PersonModel> Persons { get; set; }

        #region Overrides of Object

        /// <summary>
        /// Serves as the default hash function. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            var hash = 13;
            hash = hash + Name.GetHashCode()*7;
            hash = hash + Persons.GetHashCode()*7;

            return hash;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;

            var roleModel = obj as RoleModel;
            if (roleModel == null) return false;

            if (!string.Equals(Name, roleModel.Name, StringComparison.OrdinalIgnoreCase) && Persons.Count == roleModel.Persons.Count) return false;

            return Persons.Keys.All(key => Equals(Persons[key], roleModel.Persons[key]));
        }

        #endregion
    }
    #endregion

    [TestClass]
    public class GuidTest
    {
        [TestMethod]
        public void Guid_Test()
        {
            // arrange
            var expected = new List<RoleModel>
            {
                new RoleModel
                {
                    Name = "Group1",
                    Persons = new Dictionary<Guid, PersonModel>
                    {
                        { Guid.NewGuid(), new PersonModel { FirstName = "FirstName1", LastName = "LastName1" } },
                        { Guid.NewGuid(), new PersonModel { FirstName = "FirstName2", LastName = "LastName2" } }
                    }
                },
                new RoleModel
                {
                    Name = "Group2",
                    Persons = new Dictionary<Guid, PersonModel>
                    {
                        { Guid.NewGuid(), new PersonModel { FirstName = "FirstName3", LastName = "LastName3" } },
                        { Guid.NewGuid(), new PersonModel { FirstName = "FirstName4", LastName = "LastName4" } }
                    }
                }
            };

            using (var file = new TempFile())
            {
                // act
                using (var db = new LiteDatabase(file.Filename))
                {
                    var col = db.GetCollection<RoleModel>("roles");

                    col.Insert(expected[0]);
                    col.Insert(expected[1]);
                }

                // assert
                using (var db = new LiteDatabase(file.Filename))
                {
                    var col = db.GetCollection<RoleModel>("roles");
                    Assert.AreEqual(2, col.Count());
                    Assert.AreEqual(expected[0], col.FindById("Group1"));
                    Assert.AreEqual(expected[1], col.FindById("Group2"));
                }
            }
        }
    }
}
