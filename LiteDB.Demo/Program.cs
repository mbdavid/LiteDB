using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("LITE DB v5");
            Console.WriteLine("===========================================================");

            var cn = @"filename=d:\appPWD.db; password=abc";

            File.Delete(@"d:\appPWD.db");
            File.Delete(@"d:\appPWD-log.db");

            //using (var repo = new LiteRepository(cn))
            //{
            //    repo.Database.UserVersion = 99;
            //}
            using (var e = new LiteEngine(new EngineSettings { Filename = @"d:\appPWD.db", Password = "abc" }))
            {
                e.DbParam("USERVERSION", 199);

                e.Insert("col1", new BsonDocument[] { new BsonDocument { ["_id"] = 1, ["n"] = "Mauricio" } }, BsonAutoId.Int32);

                //e.Checkpoint();
            }

            using (var repo = new LiteRepository(cn))
            {
                var u = repo.Database.UserVersion;

                Console.WriteLine(u);

                var mau = repo.FirstOrDefault<BsonDocument>(x => x["_id"] == 1, "col1");
                //
                Console.WriteLine("dados:" + mau["n"].AsString);

            }



            Console.WriteLine(" ===========================================================");
            Console.WriteLine("End");
            Console.ReadKey();
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string City { get; set; }
        public string Name { get; set; }
        public List<User> Children { get; set; }
    }



}
