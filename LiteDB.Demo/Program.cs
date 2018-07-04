using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LiteDB;

namespace LiteDB.Demo
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var orig = @"C:\Temp\service.c.db";

            var docs = JsonSerializer.DeserializeArray("").Select(x => x.AsDocument);


            var report = LiteEngine.Recovery(orig);

            Console.WriteLine("Recovery Report:\n" + report);

            //using (var db = new LiteEngine(@"C:\Temp\SessionDatabase-recovery.ldb"))
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