# LiteDB v4

## Changes
- Fix simple lock control (multi-read/single write) in thread/process. Removed reserved lock state [OK]
- Upgrade to VS2017 [OK]
- Remove physical journal file (store journal pages after file ends) [OK]
- Remove transactions [OK] **BREAK API**
- Remove auto-id register function for custom type [OK] **BREAK API**
- Add auto-id in engine level with pre-defined commom types [OK]
- Add collection sequence (`ulong`) to use in engine level auto-id [OK]
- Add integrety check in TempFile for tests (before delete)
- Remove index definitions on mapper (fluent/attribute) [OK] **OBSOLETE**
- Review shell app and now keeps datafile open [OK]
- Auto-id default true to `_id` with `BsonType` = `ObjectId`, `Guid`, `DateTime`, `Int32` or `Int64`
- LiteDB.Perf exe proejct for concurrency examples [OK]
- Bugfix upload from local disk on storage [OK]
- Bugfix debug messages in console on shell [OK]
- Add include in engine/document level with any level [OK]

## Better query engine
- Remove auto create index on query execution. If the index is not found do full scan search (use `EnsureIndex` on initialize database) [OK]
- Implement FilterDocument option in all query implementations (full scan document) [OK]
- In `Query.And` use only one index side with full scan on other [OK]
- Print query execution plan in Query.ToString() [OK] `(Seek([Age] > 10) and Scan([Name] startsWith "John"))`
- Convert `Query.And` to `Query.Between` when possible [OK]
- Add support to `Query.Between` open/close interval [OK]
- QueryLinq for non resolved linq expression on visitor [OK] `col.Find(x => x.Id < 10 && x.Name.Length > 10)`
- BUG: Remove return Duplicate values in MultiKey indexes [OK]
- Support expression on index [OK]
- Support expression on full search [OK]

# Next
- Create compiler variables: NET35, NET40, NETFULL and NETSTANDARD.
    - Use NET35 for Unity using Reflectin.Emit
    - Use !NET35 for Reflection.Expression
    ...
    - target versions: net35, net40 and netstandard


# Finish review
- BUG: _id with date never found (milliseconds). Test using db.col.update a=5

- Add more Expression function (see RDLC functions)
- Add better error messages on parser LiteExpression (expose as public)
- Shell commands exceptions
- Find old version about database usage to add in .Info()
- Review all LiteException messages/codes
- Count\Exists when use filter must call checkTrans
- Review AND/OR index/filter
- Review if it's better use None/Flush/WriteThrough
- Review Log messages
- Review trans.CheckPoint() (do just after foreach);
- Query#Run right way to detect when Field are Expression? Create 2 declaration types?
- Implement StringScanner read string '/" and ([{ bracket (LiteExpression)
- Remove Update? Or remove from StringConnection? Create LiteDB.Upgrade.dll (to be used in shell or any app?)

# Add only in 4.1
- Implement Parent in BsonValue (how/when set?) => What do in case a same BsonValue in 2 Documents?
- Batch operation

# Document Update (4.1)

// option 1
doc.Update({ 
    "$.name": "john",
    "$": { "name": "john" },
    "$": { "name": { $expr: "$.OldName" } },
    "$.Items": 123,
    "$.Items": { $remove: true }
    "$.Items[0]": { $remove: true }
})

// option 2
doc.Update({
    "Name": "John".
    "Age": { $expr: "COUNT($.Age)" },
    $remove: ["$.Name, $.Books[*].Title"]
})

====================================================    
    

# LiteDB - A .NET NoSQL Document Store in a single data file

[![Join the chat at https://gitter.im/mbdavid/LiteDB](https://badges.gitter.im/mbdavid/LiteDB.svg)](https://gitter.im/mbdavid/LiteDB?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![Build status](https://ci.appveyor.com/api/projects/status/sfe8he0vik18m033?svg=true)](https://ci.appveyor.com/project/mbdavid/litedb) [![Build Status](https://travis-ci.org/mbdavid/LiteDB.svg?branch=master)](https://travis-ci.org/mbdavid/LiteDB)

LiteDB is a small, fast and lightweight NoSQL embedded database. 

- Serverless NoSQL Document Store
- Simple API similar to MongoDB
- 100% C# code for .NET 3.5 / NETStandard 1.3 in a single DLL (less than 200kb)
- Support for Portable UWP/PCL (thanks to @negue and @szurgot)
- Thread safe and process safe
- ACID in document level
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

## New in 3.5
- Fix concurrency problems
- Remove transaction
- Support for full scan search and LINQ
(see "How upgrade to 3.5")

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
