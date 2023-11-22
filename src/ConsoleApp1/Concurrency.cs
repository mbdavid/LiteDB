// SETUP //////////////////
const string VER = "v6";
var INSERT_1 = new Range(1, 20_000);
var COLS = new Range(1, 20);
////////////////////////

// DATASETS
var insert1 = GetData(INSERT_1, 100, 300).ToArray();

// INITIALIZE
var filename = @$"C:\LiteDB\temp\test-{VER}-{DateTime.Now.Ticks}.db";
var settings = new EngineSettings { Filename = filename };
var db = new LiteEngine(settings);

// OPEN
await db.OpenAsync();

/*
var doc = new BsonDocument
{
    ["_id"] = i,
    ["name"] = Faker.Fullname(),
    ["age"] = Faker.Age(),
    ["created"] = Faker.Birthday(),
    ["country"] = BsonDocument.DbRef(Faker.Next(1, 10), "col2"),
    ["lorem"] = Faker.Lorem(lorem, loremEnd)
};
*/

// RUN 

for (var i = COLS.Start.Value; i <= COLS.End.Value; i++)
{
    await db.RunAsync($"Create Collection 'col{i}'", $"CREATE COLLECTION col{i}");
}

IEnumerable<Task> GetTasks()
{
    for (var i = COLS.Start.Value; i <= COLS.End.Value; i++)
    {
        yield return db.RunAsync($"Insert col{i} {insert1.Length:n0}", $"INSERT INTO col{i} VALUES @0", insert1);
    }
}

await Task.WhenAll(GetTasks());


//await db.RunAsync($"Checkpoint", $"CHECKPOINT");

//db.Dump();

// reload engine
//await db.ShutdownAsync(); await db.OpenAsync();

await db.RunQueryAsync(100, "Query col1", "SELECT age, COUNT(age) FROM col1 GROUP BY age");

//db.Dump();

// SHUTDOWN
await db.ShutdownAsync();
db.Dispose();

// PRINT
Console.WriteLine();
Profiler.PrintResults(filename);

#if DEBUG
Console.WriteLine($"# DEBUG - {VER}");
#else
Console.WriteLine($"# RELEASE - {VER}");
#endif
Console.ReadKey();
