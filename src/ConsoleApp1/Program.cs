// SETUP //////////////////
const string VER = "v6";
var INSERT_1 = new Range(1, 1000);
var DELETE_1 = new Range(1, 40_000);
var INSERT_2 = new Range(1, 30_000);
////////////////////////

// DATASETS
var insert1 = GetData(INSERT_1, 100, 300).ToArray();
var insert2 = GetData(INSERT_2, 5, 10).ToArray();

var delete1 = Enumerable.Range(DELETE_1.Start.Value, DELETE_1.End.Value).Select(x => new BsonInt32(x)).ToArray();

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

await db.RunAsync($"Create Collection 'col1'", "PRAGMA USER_VERSION = 25");


await db.RunAsync($"Create Collection 'col1'", "CREATE COLLECTION col1");

await db.RunAsync($"Insert col1 {insert1.Length:n0}", "INSERT INTO col1 VALUES @0", insert1);

await db.RunQueryAsync(10, $"Query1", @"SELECT COUNT(_id) contador, contador + 1000 'contador_mais_mil' FROM col1 WHERE age = 32");

await db.RunAsync($"Rename Collection", "RENAME COLLECTION col1 TO new_col");

await db.RunQueryAsync(10, $"Query1", @"SELECT COUNT(_id) FROM new_col WHERE age = 32");

//db.Dump();

// SHUTDOWN
await db.ShutdownAsync();
db.Dispose();

// PRINT
Console.WriteLine();
//Profiler.PrintResults(filename);

#if DEBUG
Console.WriteLine($"# DEBUG - {VER}");
#else
Console.WriteLine($"# RELEASE - {VER}");
#endif
//Console.ReadKey();
