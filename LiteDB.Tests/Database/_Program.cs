using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System;

namespace LiteDB.Tests
{
    [TestClass]
    public class ProgramTest
    {
        [TestMethod]
        public void Program_Test()
        {
            var conn = new Connection();
            var work = GenerateTests(10, conn);
            Console.WriteLine("Test 1");
            //this work fine
            CheckById(work, conn);
            Console.WriteLine("Test 2 with lambda");
            //this not
            CheckByIdWithLamdba(work, conn);
        }

        //Just testdata
        static List<Guid> GenerateTests(int count, Connection conn)
        {
            var result = new List<Guid>();
            while (count != 0)
            {
                count--;
                Test test = new Test();
                test.Id = Guid.NewGuid();
                test.RandomInt = 12;
                result.Add(test.Id);
                Wrapper<Test> wrapp = new Wrapper<Test>();
                wrapp.Id = Guid.NewGuid();
                wrapp.TestId = test.Id;
                wrapp.Entry = test;
                conn.Add(wrapp);
            }

            return result;
        }

        //Find all inseted objects by Id
        static void CheckById(List<Guid> guids, Connection conn)
        {
            foreach (var item in guids)
            {
                try
                {
                    if (conn.GetById<Test>(item).Count != 0)
                    {
                        Console.WriteLine("{0} found!", item);
                    }
                    else
                    {
                        Console.WriteLine("{0} not found!", item);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        // Same with lambda
        static void CheckByIdWithLamdba(List<Guid> guids, Connection conn)
        {
            foreach (var item in guids)
            {
                try
                {
                    if (conn.GetByLambda<Test>(t => t.Id == item).Count != 0)
                    {
                        Console.WriteLine("{0} found!", item);
                    }
                    else
                    {
                        Console.WriteLine("{0} not found!", item);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }

    public class Test
    {
        private Guid _id;
        private int _randomInt;

        public Guid Id
        {
            get
            {
                return _id;
            }

            set
            {
                _id = value;
            }
        }

        public int RandomInt
        {
            get
            {
                return _randomInt;
            }

            set
            {
                _randomInt = value;
            }
        }
    }

    class Connection
    {
        string _dbpath = @"Test.db";

        // In my project I am using reflection to get Id from T.
        public void Add<T>(T something)
        {
            using (var db = new LiteDatabase(_dbpath))
            {
                var table = db.GetCollection<T>(typeof(T).Name);
                table.Insert(something);
            }
        }

        public List<T> GetById<T>(Guid id)
        {
            using (var db = new LiteDatabase(_dbpath))
            {
                var table = db.GetCollection<Wrapper<T>>(typeof(T).Name);
                var data = table.Find(x => x.TestId == id).ToList();
                var result = new List<T>();
                foreach (var item in data)
                {
                    result.Add(item.Entry);
                }

                return result;
            }
        }

        public List<T> GetByLambda<T>(Expression<Func<T, bool>> lambda)
        {
            using (var db = new LiteDatabase(_dbpath))
            {
                var table = db.GetCollection<T>(typeof(T).Name);
                // here is problem
                // this does not work
                //Expression<Func<T, bool>> ex = w => lambda(w.Entry);
                var data = table.Find(lambda).ToList();
                var result = new List<T>();
                foreach (var item in data)
                {
                    result.Add(item.Entry);
                }

                return result;
            }
        }

    }
}