using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Demo
{
    class Program
    {
        public static string _var = "nao chamou o MudaVariavel()";

        static void Main(string[] args)
        {
            var sw = new Stopwatch();

            //Debug.Assert(MudaVariavel());


            Console.WriteLine(_var);

            //TestMemoryFile.Run(sw);
            //TestAesEncryption.CreateEncryptedFile(sw);
            //TestAesEncryption.ReadEncryptedFile(sw);
            //TestDataPage.Run(sw);
            TestEngine.Run(sw);

            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds);
            Console.ReadKey();

        }

        static bool MudaVariavel()
        {
            _var = "alterou no metodo";

            return false;
        }
    }

}
