using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            var index = 0;

            sw.Start();

            using (var db = new LiteDatabase("d:/1milion.db"))
            {
                var r = db.Execute("SELECT $ FROM datagen --GROUP BY city");

                while(r.Read() && ++index < 100000)
                {
                    var rr = r.Current;
                }
            }

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadKey();

        }
    }

}
