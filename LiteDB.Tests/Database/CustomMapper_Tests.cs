using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LiteDB.Tests.Database
{
    #region Model

    public interface IBase
    {
        Guid Id { get; set; }
        string Name { get; set; }
    }

    #region for test1

    public interface IProject : IBase
    {
        string PorjectType { get; set; }
        List<ISystem> Systems { get; set; }
    }

    public interface ISystem : IBase
    {
        string SysDefine { get; set; }
        string SysMode { get; set; }
    }

    public class ProjectA : IProject
    {
        public ProjectA()
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        public string PorjectType { get; set; }
        public List<ISystem> Systems { get; set; }
    }

    public class SystemA : ISystem
    {
        public SystemA()
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        public string SysDefine { get; set; }
        public string SysMode { get; set; }
    }

    public class SystemB : ISystem
    {
        public SystemB()
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        public string SysDefine { get; set; }
        public string SysMode { get; set; }

        public List<object> ItemsList { get; set; }
    }

    #endregion

    #region for test2

    public interface IProject2 : IBase
    {
        string PorjectType { get; set; }
        List<ISystem2> Systems { get; set; }
    }


    public interface ISystem2 : IBase
    {
        Guid SysGuid { get; set; }
        string SysDefine { get; set; }
        string SysMode { get; set; }
    }

    public class ProjectA2 : IProject2
    {
        public ProjectA2()
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        public string PorjectType { get; set; }
        public List<ISystem2> Systems { get; set; }
    }


    public class SystemA2 : ISystem2
    {
        public SystemA2()
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SysGuid { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        public string SysDefine { get; set; }
        public string SysMode { get; set; }
    }

    public class SystemB2 : ISystem2
    {
        public SystemB2()
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SysGuid { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        public string SysDefine { get; set; }
        public string SysMode { get; set; }

        public List<object> ItemsList { get; set; }
    }

    #endregion

    #endregion

    public class CustomMapper : BsonMapper
    {
        protected override IEnumerable<MemberInfo> GetTypeMembers(Type type)
        {
            var list = new List<MemberInfo>(base.GetTypeMembers(type));

            if (type.IsInterface)
            {
                foreach(var @interface in type.GetInterfaces())
                {
                    list.AddRange(this.GetTypeMembers(@interface));
                }
            }

            return list;
        }
    }

    [TestClass]
    public class CustomMapper_Tests
    {
        [TestMethod]
        public void CustomMapper_Test()
        {
            // must create a new mapper (use in a static place)
            var mapper = new CustomMapper();

            // must map your Interface id
            mapper.Entity<ISystem>()
                .Id(x => x.Id);

            mapper.Entity<ProjectA>()
                .Id(x => x.Id)
                .DbRef(x => x.Systems, "systems");

            using (var db = new LiteDatabase(new MemoryStream(), mapper))
            {
                SystemA sysA = new SystemA() { Name = "SystemA", SysDefine = "SystemA Define", SysMode = "A mode" };
                SystemB sysB = new SystemB() { Name = "SystemB", SysDefine = "system B define", SysMode = "B mode", ItemsList = new List<object>() { 123 } };

                ProjectA proj = new ProjectA() { Name = "Project1", PorjectType = "ProjectType", Systems = new List<ISystem>() { sysA, sysB } };

                var proCol = db.GetCollection<ProjectA>("pros");
                var sysCol = db.GetCollection<ISystem>("systems");

                var j = mapper.ToDocument<ProjectA>(proj).ToString();

                proCol.Insert(proj);
                sysCol.InsertBulk(proj.Systems);

                var projects = proCol.Include(x => x.Systems).FindAll().ToList();

                Assert.AreEqual("SystemA", projects[0].Systems[0].Name);
            }
        }
    }
}
