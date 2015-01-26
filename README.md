# LiteDB - A .NET NoSQL Document Store in a single data file

LiteDB is a small, fast and lightweight NoSQL embedded database. 

- Serverless NoSQL Document Store
- Simple API similar to MongoDB
- 100% C# code for .NET 3.5 in a single DLL - install from NuGet: `Install-Package LiteDB`
- Transaction control - ACID
- Recovery in writing failure (journal mode)
- Store POCO classes or BsonDocument
- Store files and stream data (like GridFS in MongoDB)
- Single data file storage (like SQLite)
- Index document fields for fast search (up to 16 indexes per collection)
- Inital LINQ support for queries
- Shell command line - [try this online version](http://litedb.azurewebsites.net/)
- Open source and free for everyone - including commercial use

## Try online

[Try LiteDB Web Shell](http://litedb.azurewebsites.net/). For security reasons, in online version not all commands are available. Try offline version for full features tests.

## How to install

LiteDB is a serverless database, so there is no install. Just copy LiteDB.dll to your Bin folder and add as Reference. If you prefer, you can use NuGet package: `Install-Package LiteDB`. If you are running in a web environment, be sure that IIS user has write permission on data folder.

## How to use

A quick example for store and search documents:

```C#
// Open data file (or create if not exits)
using(var db = new LiteEngine(@"C:\Temp\MyData.db"))
{
    // Get a collection (or create, if not exits)
    var col = db.GetCollection<Customer>("customers");
    
    var customer = new Customer { Id = 1, Name = "John Doe" };

	// Insert new customer document
	col.Insert(customer);
    
    // Update a document inside a collection
    customer.Name = "Joana Doe";
    
    col.Update(customer);
    
	// Index document using a document property
	col.EnsureIndex(x => x.Name);

    // Simple Linq support
	var result = col.Find(x => x.Name.StartsWith("Jo"));
}
```

## Where to use?

- Desktop/local small applications
- Application file format
- Small web applications
- One database **per account/user** data store
- Few concurrency write users operations

# LiteDB Guide

LiteDB is a NoSQL Database based on a document store: a simple  very simple API similar to MongoDB C# official driver.

## Documents

LiteDB works with documents to store and retrive data inside data file. Your document definition can be a POCO class or BsonDocument class. In both case, LiteDB will convert your document in a BSON format to store inside disk.

BSON is a Binary JSON, a serialization for store data objects as binary array. In BSON, we have more data types than JSON. LiteDB supports `Null`, `Array`, `Object`, `Byte`, `ByteArray`, `Char`, `Boolean`, `String`, `Short`, `Int`, `Long`, `UShort`, `UInt`, `ULong`, `Float`, `Double`, `Decimal`, `DateTime`, `Guid`.

In LiteDB, documents are limited in 256Kb.

### Documents using POCO class

POCO class are simple C# classes using only `get/set` properties. It's the best way to create a strong typed documents. Your class must have a `Id` property to LiteDB identify your document. You can use `Id` named property or decorate a property with `[BsonId]` attribute. Your `Id` value must be a unique and not null. Also, `Id` data type must be a valid indexed data type. See Index section.

``` C#
// A poco entity, must have Id
public class Customer
{
    [BsonId]
	public Guid Id { get; set; }
	public string Name { get; set; }
	public List<Phone> Phones { get; set; }
}

// It's not a entity, don't need Id
public class Phone
{
    public int Code { get; set; }
    public string Number { get; set; }
    public PhoneType Type { get; set; }
}

public enum PhoneType { Mobile, Landline }
``` 

- Internal, document id is represent as `_id` property
- Do not use complex data types (like `DataSet`, `DataTable`)
- Do not use disposable objects (like `Stream`, `Graphics`)
- Enums will be converted in strings when serialized

### Documents using BsonDocument

BsonDocument is a special class that maps any document with a internal `Dictionary<string, object>`. Is very useful to read a unknown document type or use as a generic document.

```C#
// Create a BsonDocument for Customer with phones
var doc = new BsonDocument();
doc.Id = Guid.NewGuid();
doc["Name"] = "John Doe";
doc["Phones"] = new BsonArray();
doc["Phones"].Add(new BsonObject());
doc["Phones"][0]["Code"] = 55;
doc["Phones"][0]["Number"] = "(51) 8000-1234";
doc["Phones"][0]["Type"] = "Mobile";
```

With BsonDocument you can create any complex document schema.

## Collections - the store

LiteDB organize documents in stores (called in LiteDB as collections). Each collection has a unique name and contains documents with same schema/type. You can get a strong typed collection or a generic BsonDocument collections, using `GetCollection` from `LiteEngine` instance.

```C#
var db = new LiteEngine(stringConnection);

// Get a strong typed collection
var customers = db.GetCollection<Customer>("Customers");

// Get a BsonDocument collection 
var customers = db.GetCollection("Customers");
```

Collection contains all method to manipulate documents:

* `Insert` - Insert a new document
* `FindById` , `FindOne` or `Find` - Find a document using Query object or LINQ expression. At this point, only simple LINQ are supported - attribute on left, value on right side.
* `Update` - Update a document
* `Delete` - Delete a document id or using a query
* `Include` - Use include to populate properties based on others collections
* `EnsureIndex` - Create a index if not exists. All queries must have a index.

## Query

In LiteDB, queries use indexes to search documents. You can use `Query` helper or Linq expressions.

```C#
var customers = db.GetCollection<Customer>("customers");

// Create a new index (if not exists)
customers.EnsureIndex("Name");

// Query documents using 'Name' index
var results = customers.Find(Query.StartsWith("Name", "John"));

// Or using Linq
var results = customers.Find(x > x.Name.StartsWith("John"));

// Return document by ID (PK index)
var customer = customers.FindById(1);

// Count only documents where ID >= 2
var count = customers.Count(Query.GTE("_id", 2));

// All query results returns an IEnumerable<T>, so you can use Linq after too
var linq = customers.Find(x => x.Salary > 500 && x.Salary < 1000) // indexed query 
    .Where(x => x.LastName.Length > 5 && x.Age > 22) // in memory query
    .Select(x => new { x.Name, x.Salary })
    .OrderBy(x => x.Name);
```

`Query` class supports `All`, `Equals`, `Not`, `GreaterThan`, `LessThan`, `Between`, `In`, `StartsWtih` and `OR`.
All operations need an index to be executed.

## Transactions

LiteDB is atomic in transaction level. All write operations are executed inside a transaction. If you do not use `BeginTrans` and `Commit` methods, transaction are implicit for each operation.

For simplicity, LiteDB do not support concurrency transactions. LiteDB locks your datafile to guarantee that 2 users are not changing data at same time. So, do not use big transactions operations or keep a open transaction without commit or rollback.

### Fail tolerance - Journaling

After commit method called, LiteDB store all dirty pages to disk. This operations is a fail torelance. Before write direct to disk, LiteDB create a temp file (called journal file) to store all dirty pages. If there is any error during write data file, journaling save a redo log file with database dirty pages, to recovery your datafile when datafile open again. 

```C#
using(var db = new LiteEngine(dbpath))
{
    db.BeginTrans();
    
    // Do many write operations (insert, updates, deletes),
    //   but if throw any error during this operations, a Rollback() will be called automatic
    
    db.Commit();
}
```

## Storing Files

Sametimes we need store files in database. For this, LiteDB has a special `Files` collection to store files without document size limit (file limit is 2Gb per file). It's works like MongoDB `GridFS`.

```C#
// Storing a file stream inside database
db.Files.Upload("my_key.png", stream);

// Get file reference using file id
var file = db.Files.FindById("my_key.png");

// Find all files using StartsWith
var files = db.Files.Find("my_");

// Get file stream
var stream = file.OpenRead();

// Write file stream in a external stream
db.Files.Download("my_key.png", stream);
```

## Connection String

Connection string options to initialize LiteEngine class:

- **Filename**: Path for datafile. You can use only path as connection string (required)
- **Timeout**: timeout for wait for unlock datafile (default: 00:01:00)
- **Journal**: Enabled journal mode - recovery support (default: true)

## Dependency

LiteDB has no external dependency, but use [fastBinaryJson](http://fastbinaryjson.codeplex.com/) as BSON serializer 
from/to .NET objects. All source are included inside LiteDB source.

## Roadmap

Currently, LiteDB is in testing version and are not full ready for production. Please, be careful on use.

Same features/ideas for future

- Compound index: one index for multiple fields
- Multikey index: index for array values
- Full text search
