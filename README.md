# LiteDB - A .NET NoSQL Document Store in a single data file

## > v2.0.0-rc (for 1.0.x version, see `Tags`)

LiteDB is a small, fast and lightweight NoSQL embedded database. 

- Serverless NoSQL Document Store
- Simple API similar to MongoDB
- 100% C# code for .NET 3.5 in a single DLL (less than 200kb)
- ACID document transactions
- Data recovery after write failure (journal mode)
- Datafile encryption using DES cryptography
- Map your POCO classes to `BsonDocument`
- Store files and stream data (like GridFS in MongoDB)
- Single data file storage (like SQLite)
- Index document fields for fast search (up to 16 indexes per collection)
- LINQ support for queries
- Shell command line - [try this online version](http://www.litedb.org/#shell)
- Open source and free for everyone - including commercial use
- Install from NuGet: `Install-Package LiteDB`

## New features in v2.0.0
- Generic data access - can use any `Stream`
- Better mapping of classes from your entity to `BsonDocument` (like EntityFramework)
- Better cross reference with `DbRef` mapping
- ThreadSafe / ProcessSafe
- Lazy engine load (open the datafile only when running a command)
- Reduce your database size with shrink
- Support for `Initial Size` and `Limit Size` databases
- Complete re-write of engine classes with full debug logger
- See more examples at http://www.litedb.org/

## Try online

[Try LiteDB Web Shell](http://www.litedb.org/#shell). For security reasons, in the online version not all commands are available. Try the offline version for full feature tests.

## Documentation

Visit [the Wiki](https://github.com/mbdavid/LiteDB/wiki) for full documentation (v1.x)

## Download

Download the source code or binary only in [LiteDB Releases](https://github.com/mbdavid/LiteDB/releases)

## How to use LiteDB

A quick example for storing and searching documents:

```C#
// Create your POCO class
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string[] Phones { get; set; }
    public bool IsActive { get; set; }
}

// Open database (or create if doesn't exist)
using(var db = new LiteDatabase(@"C:\Temp\MyData.db"))
{
	// Get customer collection
	var col = db.GetCollection<Customer>("customers");

    // Create your new customer instance
	var customer = new Customer
    { 
        Name = "John Doe", 
        Phones = new string[] { "8000-0000", "9000-0000" }, 
        IsActive = true
    };
	
	// Insert new customer document (Id will be auto-incremented)
	col.Insert(customer);
	
	// Update a document inside a collection
	customer.Name = "Joana Doe";
	
	col.Update(customer);
	
	// Index document using a document property
	col.EnsureIndex(x => x.Name);
	
	// Use Linq to query documents
	var results = col.Find(x => x.Name.StartsWith("Jo"));
}
```

## Where to use?

- Desktop/local small applications
- Application file format
- Small web applications
- One database **per account/user** data store
- Few concurrent write operations


## Changelog

Change details for each release are documented in the [release notes](https://github.com/mbdavid/LiteDB/releases).

## License

[MIT](http://opensource.org/licenses/MIT)

Copyright (c) 2016 - Maurício David
