using LiteDB;

var fakeContent = new byte[] {
    1,22,222,184,3,227,126,129,205,182,182,143,201,181,242,107,36,
    0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
    20,88,18,70,65,77,202,50,184,177,167,59,80,255,67,66,20,
    88,18,70,65,77,202,50,184,177,167,59,80,255,67,66
};

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

    // create log file with fake content
    using (var logFile = File.Create(logfilename, 8192, FileOptions.None))
    {
        logFile.Write(fakeContent, 0, fakeContent.Length);
    }

    // do checkpoint
    db.Checkpoint();
}

Console.WriteLine("Done");


public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}