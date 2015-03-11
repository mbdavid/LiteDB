# LiteDB - A .NET NoSQL Document Store in a single data file

LiteDB is a small, fast and lightweight NoSQL embedded database for .NET. 

- Serverless NoSQL Document Store
- Simple API similar to MongoDB
- 100% C# code for .NET 4 in a single DLL (less then 200kb)
- Transaction control - ACID
- Recovery in writing failure (journal mode)
- Store POCO classes or BsonDocument
- Store files and stream data (like GridFS in MongoDB)
- Single data file storage (like SQLite)
- Index document fields for fast search (up to 16 indexes per collection)
- Inital LINQ support for queries
- Shell command line - [try this online version](http://litedb.azurewebsites.net/)
- Open source and free for everyone - including commercial use
- Install from NuGet: `Install-Package LiteDB` or download at [GitHub](https://github.com/mbdavid/LiteDB/releases)

## Try online shell

[Try LiteDB Web Shell](http://litedb.azurewebsites.net/). This online version do not have all commands. Try offline version for full features.

## How to install

LiteDB is a serverless database, so there is no install. Just copy [LiteDB.dll](https://github.com/mbdavid/LiteDB/releases)  to your Bin folder and add as Reference. If you prefer, you can use NuGet package: `Install-Package LiteDB`. If you are running in a web environment, be sure that IIS user has read/write permissions on your data folder.

## How to use

A quick example for store and search documents:

```C#
// Open data file (or create if not exits)
using(var db = new LiteDatabase(@"C:\Temp\MyData.db"))
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


## The MIT License (MIT)

Copyright (c) 2014-2015 Mauricio David

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
