# Version 3.5?

- Remove physical journal file [OK]
- Remove transactions [OK] **BREAK API**
- Remove auto-create index [OK]
- Remove auto-id in entity level (Database) [OK] **BREAK API**
- Fix simple Lock system (only multi-read/single write) [OK]
- Fix debug messages in console [OK]
- Add integrety check in TempFile for tests (before delete)
- Fix upload from local disk [OK]
- Add collection Sequence (ulong) [OK]
- Support for many BsonTypes on missing _id (not only ObjectId) [OK]
- Upsert doest work with PK AutoId _id [OK]  ** BREAK API**
- Auto-id in T class ** BREAK API**
- ** BREAK API ** - If you mark as AutoId attribute/fluentAPI, ALWAYS generate new value!! (do not test)
- Remove index definitions on mapper (fluent/attribute) [OK]
- Removed QueryBetween and convert to And(GTE,LTE) [OK]

*** Cache results in query before send to client: READ LOCK control ***

- Review if it's better use None/Flush/WriteThrough
- Multikey search without index
- Implement Lock in StreamDiskService using a second MemoryStream _locker

CONCURRENCY
- Work LiteDB.Perf [OK]

 
QUERY

- Visitor must return Query with QueryLinq when not possible convert Expression to predefined Query
- QueryAnd must return Left only with FilterDocument on right
- Query.UseIndex and UseFilter
- Query.ToString => (I(_id > 1) AND F(_id < 10))
- Remove QueryBetween = convert into QueryAnd(Query.GTE, Query.LTE)
- Count\Exists when use filter must call checkTrans

FIND MODIFY
db.Update(Query query, Action<BsonDocument> update, int skip = 0, int limit = int.MaxValue)


# PRO
- In log, support for Func<> on parameter to not execute when log are not in use
    _log(QUERY, "result = {0}", () => value.ToString());
- Why I need change tests to C:\Temp?    

# Tests pattern

- Filename must end with "_Tests.cs"
- Test name has no _Test on name
- Keep model in same file
- Each folder has a namespace
- Use TestCategory() == FolderName
    
====================================================    
    

# LiteDB - A .NET NoSQL Document Store in a single data file

[![Join the chat at https://gitter.im/mbdavid/LiteDB](https://badges.gitter.im/mbdavid/LiteDB.svg)](https://gitter.im/mbdavid/LiteDB?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![Build status](https://ci.appveyor.com/api/projects/status/sfe8he0vik18m033?svg=true)](https://ci.appveyor.com/project/mbdavid/litedb) [![Build Status](https://travis-ci.org/mbdavid/LiteDB.svg?branch=master)](https://travis-ci.org/mbdavid/LiteDB)

LiteDB is a small, fast and lightweight NoSQL embedded database. 

- Serverless NoSQL Document Store
- Simple API similar to MongoDB
- 100% C# code for .NET 3.5 / NETStandard 1.4 in a single DLL (less than 200kb)
- Support for Portable UWP/PCL (thanks to @negue and @szurgot)
- Thread safe and process safe
- ACID transactions
- Data recovery after write failure (journal mode)
- Datafile encryption using DES (AES) cryptography
- Map your POCO classes to `BsonDocument` using attributes or fluent mapper API
- Store files and stream data (like GridFS in MongoDB)
- Single data file storage (like SQLite)
- Index document fields for fast search (up to 16 indexes per collection)
- LINQ support for queries
- Shell command line - [try this online version](http://www.litedb.org/#shell)
- Pretty fast - [compare results with SQLite here](https://github.com/mbdavid/LiteDB-Perf)
- Open source and free for everyone - including commercial use
- Install from NuGet: `Install-Package LiteDB`

## New in 3.1
- New `LiteRepository` class to simple repository pattern data access [see here](https://github.com/mbdavid/LiteDB/wiki/LiteRepository)
- Collection names could be `null` and will be resolved by `BsonMapper.ResolveCollectionName` user function (default:  `typeof(T).Name`)

## Try online

[Try LiteDB Web Shell](http://www.litedb.org/#shell). For security reasons, in the online version not all commands are available. Try the offline version for full feature tests.

## Documentation

Visit [the Wiki](https://github.com/mbdavid/LiteDB/wiki) for full documentation. For simplified chinese version, [check here](https://github.com/lidanger/LiteDB.wiki_Translation_zh-cn).

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
    public int Age { get; set; }
    public string[] Phones { get; set; }
    public bool IsActive { get; set; }
}

// Open database (or create if doesn't exist)
using(var db = new LiteDatabase(@"MyData.db"))
{
	// Get customer collection
	var col = db.GetCollection<Customer>("customers");

    // Create your new customer instance
	var customer = new Customer
    { 
        Name = "John Doe", 
        Phones = new string[] { "8000-0000", "9000-0000" }, 
        Age = 39,
        IsActive = true
    };
    
    // Create unique index in Name field
    col.EnsureIndex(x => x.Name, true);
	
	// Insert new customer document (Id will be auto-incremented)
	col.Insert(customer);
	
	// Update a document inside a collection
	customer.Name = "Joana Doe";
	
	col.Update(customer);
		
	// Use LINQ to query documents (will create index in Age field)
	var results = col.Find(x => x.Age > 20);
}
```

Using fluent mapper and cross document reference for more complex data models

```C#
// DbRef to cross references
public class Order
{
    public ObjectId Id { get; set; }
    public DateTime OrderDate { get; set; }
	public Address ShippingAddress { get; set; }
    public Customer Customer { get; set; }
    public List<Product> Products { get; set; }
	public decimal Total => Products.Sum(p => p.Price);
}        

// Re-use mapper from global instance
var mapper = BsonMapper.Global;

// "Produts" and "Customer" are from other collections (not embedded document)
mapper.Entity<Order>()
    .DbRef(x => x.Customer, "customers")   // 1 to 1/0 reference
    .DbRef(x => x.Products, "products")    // 1 to Many reference
	.Field(x => x.ShippingAddress, "addr") // Embedded sub document
	.Index(x => x.OrderDate)               // Index this field
	.Ignore(x => x.Total);                 // Do not store this
            
using(var db = new LiteDatabase("MyOrderDatafile.db"))
{
    var orders = db.GetCollection<Order>("orders");
        
    // When query Order, includes references
    var query = orders
        .Include(x => x.Customer)
        .Include(x => x.Products) // 1 to many reference
        .Find(x => x.OrderDate <= DateTime.Now);

    // Each instance of Order will load Customer/Products references
	foreach(var order in query)
	{
		var name = order.Customer.Name;
		...
	}
                    
}

```

## Where to use?

- Desktop/local small applications
- Application file format
- Small web applications
- One database **per account/user** data store
- Few concurrent write operations

## Plugins

- A GUI tool: https://github.com/falahati/LiteDBViewer
- Lucene.NET directory: https://github.com/sheryever/LiteDBDirectory
- LINQPad support: https://github.com/adospace/litedbpad

## Changelog

Change details for each release are documented in the [release notes](https://github.com/mbdavid/LiteDB/releases).

## License

[MIT](http://opensource.org/licenses/MIT)

Copyright (c) 2017 - Maurício David

## Thanks

A special thanks to @negue and @szurgot helping with portable version and @lidanger for simplified chinese translation. 
