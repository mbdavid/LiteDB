using LiteDB;

var filename = @"c:\temp\test.litedb";
var logfilename = @"c:\temp\test-log.litedb";
var cs = $"filename={filename}; connection=shared;password=23746cn!0sd";

// clear existing files to re-run tests
File.Delete(filename);
File.Delete(logfilename);

// open database in "shared mode connection"
using (var db = new LiteDatabase(cs))
{
    var c = db.GetCollection("Customers");

    // insert single document
    c.Insert(new BsonDocument { ["Name"] = "test"});

    // show document
    var doc = c.FindAll().Single();
    Console.WriteLine(JsonSerializer.Serialize(doc));

    // create an empty log file
    var logFile = File.Create(logfilename, 8192, FileOptions.None);
    logFile.Dispose();

    // do checkpoint
    db.Checkpoint();
}




public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}