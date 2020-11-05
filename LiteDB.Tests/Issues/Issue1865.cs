using System;
using System.Collections.Generic;
using Xunit;
using System.Linq;
using System.Security.Cryptography;

namespace LiteDB.Tests.Issues
{
   
    public class Issue1865_Tests
    {
        public class Project : BaseEntity
        {
           
        }

        public class Point : BaseEntity
        {
            public BaseEntity Project { get; set; }
            public BaseEntity Parent { get; set; }
            public DateTime Start { get; internal set; }
            public DateTime End { get; internal set; }
        }

        public class BaseEntity
        {
            public ObjectId Id { get; set; } = ObjectId.NewObjectId();
            public string Name { get; set; }
        }

        [Fact]
        public void Incluced_document_types_should_be_reald()
        {
            BsonMapper.Global.Entity<Point>().DbRef(p => p.Project);
            BsonMapper.Global.Entity<Point>().DbRef(p => p.Parent);
            BsonMapper.Global.ResolveCollectionName = (s) => "activity";

            using var _database = new LiteDatabase(":memory:");

            var project = new Project() { Name = "Project" };
            var point1 = new Point { Parent = project, Project = project, Name = "Point 1", Start = DateTime.Now, End = DateTime.Now.AddDays(2) };
            var point2 = new Point { Parent = point1, Project = project, Name = "Point 2", Start = DateTime.Now, End = DateTime.Now.AddDays(2) };


            _database.GetCollection<Point>("activity").Insert(point1);
            _database.GetCollection<Point>("activity").Insert(point2);
            _database.GetCollection<Project>("activity").Insert(project);


            var p1 = _database.GetCollection<Point>()
                .FindById(point1.Id);
            Assert.Equal(typeof(Project), p1.Parent.GetType());
            Assert.Equal(typeof(Project), p1.Project.GetType());

            var p2 = _database.GetCollection<Point>()
                .FindById(point2.Id);
            Assert.Equal(typeof(Point), p2.Parent.GetType());
            Assert.Equal(typeof(Project), p2.Project.GetType());

            p1 = _database.GetCollection<Point>()
                .Include(p=>p.Parent).Include(p=>p.Project)
                .FindById(point1.Id);
            Assert.Equal(typeof(Project), p1.Parent.GetType());
            Assert.Equal(typeof(Project), p1.Project.GetType());

            p2 = _database.GetCollection<Point>()
                .Include(p => p.Parent).Include(p => p.Project)
                .FindById(point2.Id);
            Assert.Equal(typeof(Point), p2.Parent.GetType());
            Assert.Equal(typeof(Project), p2.Project.GetType());
        }
    }
}
