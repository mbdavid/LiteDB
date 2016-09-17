using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Platform;
using LiteDB.Tests.NetCore.Tests;

namespace LiteDB.Tests.NetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new EncryptedTest().Encrypted_Test();
            new AutoIdTest().AutoId_Test();
            new BsonFieldTest().BsonField_Test();
            new BsonTest().Bson_Test();
            new FileStorage_Test().FileStorage_InsertDelete();
            new FileStorage_Test().FileStorage_50files();
            new JsonTest().Json_Test();
            new LinqTest().Linq_Test();
            new LinqTest().EnumerableTest();

            Console.WriteLine("All Tests completed");
            Console.ReadKey();
        }
    }

    public static class Helper
    {
        public static void AssertIsTrue(string name, int index, bool result)
        {
            var _tmpColor = Console.ForegroundColor;

            if (result)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.Red;

            string message = result ? "success" : "error";
            Console.WriteLine($"{name}[{index}] => {message}");

            Console.ForegroundColor = _tmpColor;
        }
    }
}
