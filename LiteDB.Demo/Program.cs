using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LiteDB;

namespace LiteDB.Demo
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var orig = @"C:\Git\LiteDB\LiteDB.Shell\bin\Debug\net40\teste.db";



            using (var db = new LiteEngine(orig))
            {
                var col = db.GetCollectionNames().First();

                var tt = db.FindAll(col).ToArray();

                var c = db.FindOne("DataObjects_1", Query.EQ("_id", "qyeyeW.1oMJK5"));

                

                Console.WriteLine(c == null);

            }
                //{
                //    // reading all database
                //    foreach (var col in db.GetCollectionNames())
                //    {
                //        Console.WriteLine("Collection: " + col);
                //    
                //    
                //        foreach (var doc in db.Find(col, Query.All("Token.LastUsedOn")).Take(10))
                //        {
                //            Console.WriteLine(JsonSerializer.Serialize(doc, true));
                //            // ok
                //        }
                //    }
                //}


                Console.WriteLine("End.");
            Console.ReadKey();
        }
    }
}