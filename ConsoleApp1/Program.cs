using LiteDB;

using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var d = JsonSerializer.Deserialize(@"
            {
                _id:1, 
                nome: 'Mauricio'
            }").AsDocument;

            var e = BsonExpression.Create(@"EXTEND($, { nome: UPPER(nome) })");

            var r = e.ExecuteScalar(d);

            Console.WriteLine("   doc=" + d.ToString());
            Console.WriteLine("  expr=" + e.ToString());
            Console.WriteLine("result=" + r.ToString());

        }
    }
}
