global using LiteDB;
global using LiteDB.Engine;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.
var filename = @$"C:\LiteDB\temp\test-stress-{DateTime.Now.Ticks}.db";
var settings = new EngineSettings { Filename = filename };
var db = new LiteEngine(settings);
var rnd = new Random(420);
var cols = 5;

// OPEN
await db.OpenAsync();

// INIT DB
for (var i = 0; i < cols; i++)
{
    await db.ExecuteAsync($"CREATE COLLECTION col{i}");
}

app.MapGet("/insert", async () =>
{
    var col = rnd.Next(1, 5);

    var doc = new BsonDocument
    {
        ["name"] = Faker.Fullname(),
        ["age"] = Faker.Age(),
        ["created"] = Faker.Birthday(),
        ["lorem"] = Faker.Lorem(5)
    };

    await db.ExecuteAsync($"INSERT INTO col{col} VALUES @0", (BsonValue)doc);

});

app.Run();
