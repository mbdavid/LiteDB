# DEV-MK => MultiKey Index implementation

=> Thinks: WHY I need to specify MULTIKEY? Why multikey is not supported by all indexes? 
	# GetValue() <= IEnumerable<BsonValue>
	# If key is single, ok, index as a single value. If is an array, do for all items
	# How update document ajusting all indexes?
		- 1) Simple: delete/insert all - If I use this solution, I dont need Slot stored inside IndexNode! NOOOO - To delete an index I need this
		- 2) Diff: Get all indexes keys in array, compare with document
	# How delete a document from a Query (without using PK)?
		- Now deleted required PK for first element on list 
		- Spend 6 bytes to point DataBlock to PK? (or just query it?)

= Needs Double Linked List in IndexNodes to drop an indexs (will be slow to drop indexes)
		

# v-next
- Create some "Reserved bytes" in index page
- ReadOnly support
- Encryption only in .NET full 3.5
- Autocommit problem: duplicate key throw rollback in all

# MultiKey
- Implementar GetValues() retornando um IEnumerable<BsonValue> no BsonDocument
- Usar IndexRef[] para um novo conjunto de ponteiros
- Armazenar o array de ponteiros em um DataBlock
- Remover DocumentCount++|-- de dentro do DataService e colocar no Insert.cs|Delete.cs

# Changes to v3
- Thread Safe
- Lock file on open (single process access)
- `LiteEngine`: new low data layer access
- BsonDocument implements IDictionary, BsonArray implements IList
- Remove shell from LiteDB (avaiable only in LiteDB.Shell tool)
- Checkpoint cache - CacheSize
- Autocommit = false : Transaction control
- Remove index options
- Remove: Shrink, Encryption, ...
- New QueryFunc
- New Upsert
- New FindIndex
- Remove ChangeID (avoid write Header Page all times)
- FileStorage will support OpenWrite("fileId") <= LiteFileStream

# TODO
- netstandard 1.4
- Virtual index fields

# BsonMapper
- Support interface IBsonMapper (like JSON.NET)
- BsonMapper with ReadOnly / private setter options / Fields

# ThreadSafe
- LiteDB will be single process (ThreadSafe) - when a process open datafile will be opened with NoShare
- DbEngine still lazy load (with lazy open file)
- LiteDB will close datafile only when Dispose() LiteDatabase/DbEngine/IDiskService
- All write method DbEngine will be inside a `lock`
- Cache must be `locked` to support concurrent queries
- Cache will be keeped after read/writes :)
- No more check header page in each operation :)
- Journal file will be deleted only when disk dispose (set length = 0 to reuse)
- Still using journal file to cache memory control do not exceed 5000 pages
- No more transaction control to user (will be document acid only)
- Review Logger class = LogLevel be simpler? I want log VERBOSE

This structure will be work more close to a DBMS (centralized database instance with a server running).

# Cons
- Will not support N application running in some datafile (like many desktops apps using a server datafile)
- Console shell CLI must be always disconected?

# To think about
- Write operation can be in an async Task? Will boost performance :) (needs .NET 4 or works with Thread)
- IQueryProvider to `db.Query<MyClass>("colName").Where(x => x.IdName == "John").ToPaged(1, 10);`


=============================================================================

# LiteDB - A .NET NoSQL Document Store in a single data file

[![Join the chat at https://gitter.im/mbdavid/LiteDB](https://badges.gitter.im/mbdavid/LiteDB.svg)](https://gitter.im/mbdavid/LiteDB?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![Build status](https://ci.appveyor.com/api/projects/status/sfe8he0vik18m033?svg=true)](https://ci.appveyor.com/project/mbdavid/litedb)

LiteDB is a small, fast and lightweight NoSQL embedded database. 

- Serverless NoSQL Document Store
- Simple API similar to MongoDB
- 100% C# code for .NET 3.5 in a single DLL (less than 200kb)
- Support for Portable UWP/PCL (thanks to @negue and @szurgot)
- ACID transactions
- Data recovery after write failure (journal mode)
- Datafile encryption using DES (AES) cryptography
- Map your POCO classes to `BsonDocument` using attributes or fluent mapper API
- Store files and stream data (like GridFS in MongoDB)
- Single data file storage (like SQLite)
- Index document fields for fast search (up to 16 indexes per collection)
- LINQ support for queries
- Shell command line - [try this online version](http://www.litedb.org/#shell)
- Open source and free for everyone - including commercial use
- Install from NuGet: `Install-Package LiteDB`
- Install portable version from NuGet: `Install-Package LiteDB.Core`

## New features in v2
- Generic data access - can use any `Stream` as datafile
- Better mapping of classes from your entity to `BsonDocument` (like EntityFramework)
- Better cross reference with `DbRef` mapping
- Lazy engine load (open the datafile only when running a command)
- Reduce your database size with shrink
- Support for `Initial Size` and `Limit Size` databases
- Complete re-write of engine classes with full debug logger
- Complete re-write disk operation to be more safe
- Transaction control
- `BsonMapper.Global` class mapper definition
- See more examples at http://www.litedb.org/ and unit tests

## Try online

[Try LiteDB Web Shell](http://www.litedb.org/#shell). For security reasons, in the online version not all commands are available. Try the offline version for full feature tests.

## Documentation

Visit [the Wiki](https://github.com/mbdavid/LiteDB/wiki) for full documentation

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

## Changelog

Change details for each release are documented in the [release notes](https://github.com/mbdavid/LiteDB/releases).

## License

[MIT](http://opensource.org/licenses/MIT)

Copyright (c) 2016 - Maurício David

## Thanks

A special thanks to @negue and @szurgot helping with portable version.
