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

            sw.Start();

            using (var db = new LiteDatabase("c:/temp/app.db"))
            {
                var r = db.Execute("SELECT { city, t: COUNT(_id) } FROM zip GROUP BY city");

                while(r.Read())
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
