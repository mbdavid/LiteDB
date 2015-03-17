# LiteDB - A .NET NoSQL Document Store in a single data file

LiteDB is a small, fast and lightweight NoSQL embedded database. 

- Serverless NoSQL Document Store
- Simple API similar to MongoDB
- 100% C# code for .NET 4 in a single DLL (less then 200kb)
- Transaction control - ACID
- Recovery in writing failure (journal mode)
- Store POCO classes or `BsonDocument`
- Store files and stream data (like GridFS in MongoDB)
- Single data file storage (like SQLite)
- Index document fields for fast search (up to 16 indexes per collection)
- LINQ support for queries
- Shell command line - [try this online version](http://litedb.azurewebsites.net/)
- Open source and free for everyone - including commercial use
- Install from NuGet: `Install-Package LiteDB`

## Try online

[Try LiteDB Web Shell](http://litedb.azurewebsites.net/). For security reasons, in online version not all commands are available. Try offline version for full features tests.

## Documentation

Visit [Wiki for full documentation](https://github.com/mbdavid/LiteDB/wiki)

## Download

Download source code or binary only in [LiteDB Releases](https://github.com/mbdavid/LiteDB/releases)

## How to use LiteDB

A quick example for store and search documents:

```C#
// Create your POCO class
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string[] Phones { get; set; }
    public bool IsActive { get; set; }
}

// Open database (or create if not exits)
using(var db = new LiteDatabase(@"C:\Temp\MyData.db"))
{
	// Get a collection (or create, if not exits)
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
- Few concurrency write users operations


## Changelog

Details changes for each release are documented in the [release notes](https://github.com/mbdavid/LiteDB/releases).

## License

[MIT](http://opensource.org/licenses/MIT)

Copyright (c) 2015 - Maurício David