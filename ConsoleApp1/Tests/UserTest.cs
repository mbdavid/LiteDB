using LiteDB;
using LiteDB.Engine;

var password= "bzj2NplCbVH/bB8fxtjEC7u0unYdKHJVSmdmPgArRBwmmGw0+Wd2tE+b2zRMFcHAzoG71YIn/2Nq1EMqa5JKcQ==";
var original = "C:\\LiteDB\\Examples\\TestCacheDb.db";
var path = $"C:\\LiteDB\\Examples\\TestCacheDb_{DateTime.Now.Ticks}.db";

File.Copy(original, path);

var settings = new EngineSettings
{
    //AutoRebuild = true,
    Filename = path,
    Password = password
};

/*
var errors = new List<FileReaderError>();

using var reader = new FileReaderV8(settings, errors);

reader.Open();

var pragmas = reader.GetPragmas();
var cols = reader.GetCollections().ToArray();
var indexes = reader.GetIndexes(cols[0]);

var docs = reader.GetDocuments("hubData$AppOperations").ToArray();
*/

// /*
var db = new LiteEngine(settings);

db.Rebuild();



var reader = db.Query("hubData$AppOperations", Query.All());
var data = reader.ToList();
// */
Console.ReadKey();