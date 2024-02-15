using LiteDB;
using LiteDB.Engine;

var password = "bzj2NplCbVH/bB8fxtjEC7u0unYdKHJVSmdmPgArRBwmmGw0+Wd2tE+b2zRMFcHAzoG71YIn/2Nq1EMqa5JKcQ==";
var path = $"C:\\LiteDB\\Examples\\CrashDB_{DateTime.Now.Ticks}.db";

var settings = new EngineSettings
{
    //AutoRebuild = true,
    Filename = path,
//    Password = password
};

var data = Enumerable.Range(1, 1000).Select(i => new BsonDocument
{
    ["_id"] = i,
    ["name"] = Faker.Fullname(),
    ["age"] = Faker.Age(),
    ["created"] = Faker.Birthday(),
    ["lorem"] = Faker.Lorem(10, 30)
}).ToArray();

try
{
    // forcando erro de escrita no disco
    using (var db = new LiteEngine(settings))
    {
        db.SimulateDiskWriteFail = (page) =>
        {
            if (page.Position == 8192 * 50)
            {
                throw new IOException("Simulated disk write failure");
            }
        };

        db.Insert("col1", data, BsonAutoId.Int32);
        db.Insert("col2", data, BsonAutoId.Int32);
    }
}
catch (Exception ex)
{
    Console.WriteLine("ERROR: " + ex.Message);
}

Console.WriteLine("Recovering database...");

using (var db = new LiteEngine(settings))
{
    var col1 = db.Query("col1", Query.All()).ToList().Count;
    var col2 = db.Query("col2", Query.All()).ToList().Count;

    Console.WriteLine($"Col1: {col1}");
    Console.WriteLine($"Col1: {col2}");

}


Console.ReadKey();