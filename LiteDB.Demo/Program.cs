using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        static string PATH = @"c:\temp\memory-file.db";
        static int N = 1000000;
        static BsonDocument doc = new BsonDocument
        {
            ["_id"] = 1,
            ["name"] = "NoSQL Database",
            ["birthday"] = new DateTime(1977, 10, 30),
            ["phones"] = new BsonArray { "000000", "12345678" },
            ["active"] = true
        }; // 109b

        static void Main(string[] args)
        {
            File.Delete(PATH);

            var factory = new FileStreamDiskFactory(PATH, false);
            var file = new FileMemory(factory, true);

            Console.WriteLine("Processing... " + N);

            var sw = new Stopwatch();
            sw.Start();

            // Write documents inside data file (append)
            WriteFile(file);

            Console.WriteLine("Write: " + sw.ElapsedMilliseconds);
            Thread.Sleep(2000);
            sw.Restart();

            // Read document inside data file
            ReadFile(file);

            file.Dispose();

            sw.Stop();

            Console.WriteLine("Read: " + sw.ElapsedMilliseconds);
            Console.ReadKey();
        }

        static void ReadFile(FileMemory file)
        {
            var fileReader = file.GetReader(false);

            IEnumerable<ArraySlice<byte>> source()
            {
                var pos = 0;

                while (pos < file.Length)
                {
                    var page = fileReader.GetPage(pos);

                    pos += 8192;

                    yield return page;
                }
            };

            for (var j = 0; j < 1; j++)
            {
                using (var bufferReader = new BufferReader(source()))
                {
                    for (var i = 0; i < N; i++)
                    {
                        var d = bufferReader.ReadDocument();
                    }
                }
            }

            fileReader.Dispose();
        }

        static void WriteFile(FileMemory file)
        {
            var fileReader = file.GetReader(true);

            var dirtyPages = new List<PageBuffer>();

            IEnumerable<ArraySlice<byte>> source()
            {
                while (true)
                {
                    var page = fileReader.NewPage();

                    dirtyPages.Add(page);

                    yield return page;
                }
            };

            for (var j = 0; j < 1; j++)
            {
                using (var bufferWriter = new BufferWriter(source()))
                {
                    for (var i = 0; i < N; i++)
                    {
                        doc["_id"] = i;

                        bufferWriter.WriteDocument(doc);
                    }
                }


                file.WriteAsync(dirtyPages);

                dirtyPages.Clear();
            }

            // só posso fechar o reader apos ter enviado tudo para salvar (no caso as sujas)
            fileReader.Dispose();
        }
    }

}
