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
        static string PATH = @"c:\temp\memory-file.db";

        static void Main(string[] args)
        {
            var sw = new Stopwatch(); sw.Start();

            /*
            var l = new List<PageBuffer>();
            var p0 = new PageBuffer { Position = 1, Buffer = new ArraySegment<byte>(new byte[] { 25 }) };
            var p1 = p0;
            p1.Position = 25;
            */


            WriteFile();

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadKey();
        }

        static void ReadFile()
        {
            var factory = new FileStreamDiskFactory(PATH, false);
            var file = new FileMemory(factory, false);

            IEnumerable<ArraySegment<byte>> source()
            {
                using (var fileReader = file.GetReader())
                {
                    var pos = 0;

                    while (pos < file.Length)
                    {
                        var page = fileReader.GetPage(pos, true);

                        pos += 8192;

                        yield return page.Buffer;
                    }
                }
            };

            for (var j = 0; j < 1; j++)
            {
                var bufferReader = new BufferReader(source());

                for (var i = 0; i < 1000000; i++)
                {
                    var d = bufferReader.ReadDocument();
                }

                bufferReader.Dispose();
            }

            file.Dispose();
        }

        static void WriteFile()
        {
            File.Delete(PATH);

            var doc = new BsonDocument
            {
                ["_id"] = 1,
                ["name"] = "NoSQL Database",
                ["birthday"] = new DateTime(1977, 10, 30),
                ["phones"] = new BsonArray { "000000", "12345678" },
                ["active"] = true
            };

            var factory = new FileStreamDiskFactory(PATH, false);
            var file = new FileMemory(factory, true);

            var dirtyPages = new List<PageBuffer>();

            IEnumerable<ArraySegment<byte>> source()
            {
                using (var fileReader = file.GetReader())
                {
                    while (true)
                    {
                        var page = fileReader.NewPage();

                        dirtyPages.Add(page);

                        yield return page.Buffer;
                    }
                }
            };

            for (var j = 0; j < 1; j++)
            {
                var bufferWriter = new BufferWriter(source());

                for (var i = 0; i < 1000000; i++)
                {
                    bufferWriter.WriteDocument(doc);
                }

                bufferWriter.Dispose();

                file.WriteAsync(dirtyPages);

                dirtyPages.Clear();
            }

            file.Dispose();
        }
    }

}
