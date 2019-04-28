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

            //var json = "{a:1, nome: 'Jose', arr: [1, 2, 3, 4], items: [ { id:1, precos: [10,20] }, { id:2, precos:[40] } ]}";
            var json = "{id: 1, nomes:['jose','maria','carlos']}";

            var doc = JsonSerializer.Deserialize(json).AsDocument;

            var source = new List<BsonDocument>
            {
                JsonSerializer.Deserialize("{id: 1, nomes:['jose','maria','carlos']}").AsDocument,
                JsonSerializer.Deserialize("{id: 2, nomes:['maria']}").AsDocument,
                JsonSerializer.Deserialize("{id: 3, nomes:['jose','maria']}").AsDocument,
                JsonSerializer.Deserialize("{id: 4, nomes:['carlos']}").AsDocument,
            };

            var e = BsonExpression.Create("*");

            //e.Parameters["aa"] = 1234;

            //var s = e.ExecuteScalar(doc);
            var r = e.Execute(source).ToArray();

            //Console.WriteLine(r);

            Console.WriteLine("===========================================================");
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
