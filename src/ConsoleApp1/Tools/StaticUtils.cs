global using static StaticUtils;

using System.Numerics;
using System.Runtime.CompilerServices;

public static class StaticUtils
{
    public static IEnumerable<BsonDocument> GetData(Range range, int lorem = 5, int loremEnd = -1)
    {
        var allocated = 0L;

        Console.Write($"> {(range.End.Value - range.Start.Value + 1):n0} docs allocated in memory".PadRight(40, ' ') + ": ");

        for (var i = range.Start.Value; i <= range.End.Value; i++)
        {
            var doc = new BsonDocument
            {
                ["_id"] = i,
                ["name"] = Faker.Fullname(),
                ["age"] = Faker.Age(),
                ["created"] = Faker.Birthday(),
                ["country"] = BsonDocument.DbRef(Faker.Next(1, 10), "col2"),
                ["lorem"] = Faker.Lorem(lorem, loremEnd)
            };

            allocated += doc.GetBytesCount();

            yield return doc;
        }

        Console.WriteLine($"{(allocated / 1024 / 1024):n0} MB");

    }

    public static BsonDocument[] GetCountries() => new BsonDocument[]
    {
        new() { ["_id"] = 1, ["name"] = "Brazil", ["code"] = "BR" },
        new() { ["_id"] = 2, ["name"] = "USA", ["code"] = "US" },
        new() { ["_id"] = 3, ["name"] = "México", ["code"] = "MX" },
        new() { ["_id"] = 4, ["name"] = "Portugal", ["code"] = "PT" },
        new() { ["_id"] = 5, ["name"] = "France", ["code"] = "FR" },
        new() { ["_id"] = 6, ["name"] = "Italy", ["code"] = "IT" },
        new() { ["_id"] = 7, ["name"] = "Spain", ["code"] = "ES" },
        new() { ["_id"] = 8, ["name"] = "China", ["code"] = "CH" },
        new() { ["_id"] = 9, ["name"] = "Uruguai", ["code"] = "UR" },
        new() { ["_id"] = 10, ["name"] = "Argentina", ["code"] = "AR" },
    };

    public static Task RunAsync(this ILiteEngine db, string message, string sql, IReadOnlyList<BsonValue> args0)
    {
        var doc = new BsonDocument { ["0"] = new BsonArray(args0) };

        return RunAsync(db, message, sql, doc);
    }

    public static async Task RunAsync(this ILiteEngine engine, string message, string sql, BsonDocument? parameters = null)
    {
        var sw = Stopwatch.StartNew();

        Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

        var result = await engine.ExecuteAsync(sql, parameters ?? BsonDocument.Empty);

        Console.Write($"{sw.Elapsed.TotalMilliseconds:n0} ms");

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($" [{result:n0} affected]");
        Console.ForegroundColor = ConsoleColor.Gray;


        Profiler.AddResult(message, true);
    }

    public static Task RunQueryAsync(this ILiteEngine db, string message, string sql)
        => RunQueryAsync(db, 0, message, sql, BsonDocument.Empty);

    public static Task RunQueryAsync(this ILiteEngine db, int printTop, string message, string sql)
        => RunQueryAsync(db, printTop, message, sql, BsonDocument.Empty);

    public static async Task RunQueryAsync(this ILiteEngine db, int printTop, string message, string sql, BsonDocument parameters)
    {
        Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

        if (printTop > 0) Console.WriteLine("...");

        var sw = Stopwatch.StartNew();
        var reader = await db.ExecuteReaderAsync(sql, parameters);
        sw.Stop();

        if (printTop > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            //Console.WriteLine(reader.); GetPlan()
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        var index = 1;
        var total = 0;

        sw.Start();

        while (await reader.ReadAsync())
        {
            sw.Stop();

            var item = reader.Current;

            if (printTop > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"[{(index++):000}] ");
                Console.ForegroundColor = ConsoleColor.Cyan;

                if (item is BsonString str)
                {
                    Console.WriteLine(str.Value.MaxLength(80));
                }
                else
                {
                    Console.WriteLine(item.ToString().MaxLength(80) + (item.GetBytesCount() > 80 ? $" ({item.GetBytesCount():n0} bytes)" : ""));
                }

                Console.ForegroundColor = ConsoleColor.Gray;
                printTop--;
            }

            total++;

            sw.Start();
        }

        sw.Stop();

        Console.Write($"{sw.Elapsed.TotalMilliseconds:n0} ms");

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($" [{total:n0} document{(total > 1 ? "s" : "")}]");
        Console.ForegroundColor = ConsoleColor.Gray;

        Profiler.AddResult(message, true);
    }

    public static async Task<T> RunAsync<T>(string message, Func<Task<T>> asyncFunc)
    {
        var sw = Stopwatch.StartNew();

        Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

        var result = await asyncFunc();

        Console.WriteLine($"{sw.Elapsed.TotalMilliseconds:n0}ms");

        return result;
    }

    public static T RunSync<T>(string message, Func<T> syncFunc)
    {
        var sw = Stopwatch.StartNew();

        Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

        var result = syncFunc();

        Console.WriteLine($"{sw.Elapsed.TotalMilliseconds:n0}ms");

        return result;
    }

    public static string MaxLength(this string self, int size)
    {
        return self.Length <= size ? self : self.Substring(0, size - 3) + $"...";
    }
}